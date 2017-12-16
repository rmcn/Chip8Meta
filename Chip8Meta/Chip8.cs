using System;

namespace Chip8Meta
{
    public class Chip8
    {
        private const ushort MemBase = 0x200;

        private byte[] _mem;
        private byte[] _reg;
        private ushort _i;
        private ushort _pc;
        private byte _delay;
        private byte _sound;
        private bool[] _display;
        private ushort[] _stack;
        private int _sp;

        public Chip8()
        {
            _mem = new byte[4096];
            _reg = new byte[16];
            _pc = MemBase;
            _display = new bool[64 * 32];
            _stack = new ushort[16];
            _sp = -1;
        }

        public void Step()
        {
            var op = new Instruction(_mem[_pc], _mem[_pc+1]);
            _pc += 2;

            switch (op.N1)
            {
                // 00E0 - CLS
                // 00EE - RET
                // 0nnn - SYS addr
                // 1nnn - JP addr
                case 0x2: _sp++; _stack[_sp] = _pc; _pc = op.Addr; break; // 2nnn - CALL addr
                // 3xkk - SE Vx, byte
                // 4xkk - SNE Vx, byte
                // 5xy0 - SE Vx, Vy
                case 0x6: _reg[op.N2] = op.B2; break; // 6xkk - LD Vx, byte
                // 7xkk - ADD Vx, byte
                // 8xy0 - LD Vx, Vy
                // 8xy1 - OR Vx, Vy
                // 8xy2 - AND Vx, Vy
                // 8xy3 - XOR Vx, Vy
                // 8xy4 - ADD Vx, Vy
                // 8xy5 - SUB Vx, Vy
                // 8xy6 - SHR Vx {, Vy}
                // 8xy7 - SUBN Vx, Vy
                // 8xyE - SHL Vx {, Vy}
                // 9xy0 - SNE Vx, Vy
                case 0xA: _i = op.Addr; break; // Annn - LD I, addr
                // Bnnn - JP V0, addr
                // Cxkk - RND Vx, byte
                case 0xD: Draw(_reg[op.N2], _reg[op.N3], op.N4); break; // Dxyn - DRW Vx, Vy, nibble
                // Ex9E - SKP Vx
                // ExA1 - SKNP Vx
                // Fx07 - LD Vx, DT
                // Fx0A - LD Vx, K
                // Fx15 - LD DT, Vx
                // Fx18 - LD ST, Vx
                // Fx1E - ADD I, Vx
                // Fx29 - LD F, Vx
                // Fx33 - LD B, Vx
                // Fx55 - LD [I], Vx
                // Fx65 - LD Vx, [I]
                default:
                    throw new NotImplementedException($"Unknown instruction {op}");
            }
        }

        private void Draw(byte x, byte y, byte height)
        {
            _reg[0xF] = 0;
            for (int dy = 0; dy < height; dy++)
            {
                var spriteRow = _mem[_i + dy];
                for (int dx = 0; dx < 8; dx++)
                {
                    var shouldSetPixel = (spriteRow & (0b1000_0000 >> dx)) > 0;
                    if (shouldSetPixel)
                    {
                        var displayOffset = (y + dy) * 64 + x + dx;
                        if (_display[displayOffset])
                        {
                            _reg[0xF] = 1;
                            _display[displayOffset] = false;
                        }
                        else
                        {
                            _display[displayOffset] = true;
                        }
                    }
                }
            }
        }

        public void Load(byte[] data)
        {
            data.CopyTo(_mem, MemBase);
        }
    }

    internal struct Instruction
    {
        private byte _b1;
        private byte _b2;

        public Instruction(byte b1, byte b2)
        {
            _b1 = b1;
            _b2 = b2;
        }

        public byte N1 => (byte)((_b1 & 0xf0) >> 4);
        public byte N2 => (byte)(_b1 & 0x0f);
        public byte N3 => (byte)((_b2 & 0xf0) >> 4);
        public byte N4 => (byte)(_b2 & 0x0f);

        public byte B1 => _b1;
        public byte B2 => _b2;
        public ushort Addr => (ushort)((_b1 << 8 | _b2) & 0x0fff);

        public override string ToString() => string.Format("{0:X}{1:X}{2:X}{3:X}", N1, N2, N3, N4);
    }
}
