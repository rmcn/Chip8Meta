using System;

namespace Chip8Meta
{
    public class Chip8
    {
        private static byte[] Digits =
        {
            0xF0, 0x90, 0x90, 0x90, 0xF0, // 0
            0x20, 0x60, 0x20, 0x20, 0x70, // 1
            0xF0, 0x10, 0xF0, 0x80, 0xF0, // 2
            0xF0, 0x10, 0xF0, 0x10, 0xF0, // 3
            0x90, 0x90, 0xF0, 0x10, 0x10, // 4
            0xF0, 0x80, 0xF0, 0x10, 0xF0, // 5
            0xF0, 0x80, 0xF0, 0x90, 0xF0, // 6
            0xF0, 0x10, 0x20, 0x40, 0x40, // 7
            0xF0, 0x90, 0xF0, 0x90, 0xF0, // 8
            0xF0, 0x90, 0xF0, 0x10, 0xF0, // 9
            0xF0, 0x90, 0xF0, 0x90, 0x90, // A
            0xE0, 0x90, 0xE0, 0x90, 0xE0, // B
            0xF0, 0x80, 0x80, 0x80, 0xF0, // C
            0xE0, 0x90, 0x90, 0x90, 0xE0, // D
            0xF0, 0x80, 0xF0, 0x80, 0xF0, // E
            0xF0, 0x80, 0xF0, 0x80, 0x80  // F
        }; 

        public const int DisplayWidth = 64;
        public const int DisplayHeight = 32;

        private const ushort MemBase = 0x200;

        private Random _rand;
        private byte[] _mem;
        private byte[] _reg;
        private ushort _i;
        private ushort _pc;
        private byte _delay;
        private byte _sound;
        private bool[] _display;
        private ushort[] _stack;
        private int _sp;
        private bool[] _keys;

        public Chip8()
        {
            _mem = new byte[4096];
            Digits.CopyTo(_mem, 0);
            _reg = new byte[16];
            _pc = MemBase;
            _display = new bool[DisplayWidth * DisplayHeight];
            _stack = new ushort[16];
            _sp = -1;
            _rand = new Random();
            _keys = new bool[16];
        }

        public bool[] Display => _display;
        public bool[] Keys => _keys;

        public void Tick()
        {
            _delay--;
            _sound--;
        }

        public void Step()
        {
            var op = new Instruction(_mem[_pc], _mem[_pc+1]);
            _pc += 2;

            switch (op.N1)
            {
                case 0x0:
                    switch(op.B2)
                    {
                        // 00E0 - CLS
                        case 0xEE: _pc = _stack[_sp]; _sp--; break; // 00EE - RET
                        // 0nnn - SYS addr
                        default: throw new NotImplementedException($"Unknown instruction {op}");
                    }
                    break;
                case 0x1: _pc = op.Addr; break; // 1nnn - JP addr
                case 0x2: _sp++; _stack[_sp] = _pc; _pc = op.Addr; break; // 2nnn - CALL addr
                case 0x3: if (_reg[op.N2] == op.B2) { _pc += 2; }; break; // 3xkk - SE Vx, byte
                case 0x4: if (_reg[op.N2] != op.B2) { _pc += 2; }; break; // 4xkk - SNE Vx, byte
                // 5xy0 - SE Vx, Vy
                case 0x6: _reg[op.N2] = op.B2; break; // 6xkk - LD Vx, byte
                case 0x7: _reg[op.N2] += op.B2; break; // 7xkk - ADD Vx, byte
                case 0x8:
                    switch (op.N4)
                    {
                        case 0x0: _reg[op.N2] = _reg[op.N3]; break; // 8xy0 - LD Vx, Vy
                        // 8xy1 - OR Vx, Vy
                        case 0x2:  _reg[op.N2] = (byte)(_reg[op.N2] & _reg[op.N3]); break; // 8xy2 - AND Vx, Vy
                        // 8xy3 - XOR Vx, Vy
                        case 0x4: // 8xy4 - ADD Vx, Vy
                            _reg[0xf] = _reg[op.N2] + _reg[op.N3] < 256 ? (byte)0 : (byte)1;
                            _reg[op.N2] = (byte)(_reg[op.N2] + _reg[op.N3]);
                            break;
                        case 0x5: // 8xy5 - SUB Vx, Vy
                            _reg[0xf] = _reg[op.N2] - _reg[op.N3] < 0 ? (byte)0 : (byte)1;
                            _reg[op.N2] = (byte)(_reg[op.N2] - _reg[op.N3]);
                            break;                        
                        // 8xy6 - SHR Vx {, Vy}
                        // 8xy7 - SUBN Vx, Vy
                        // 8xyE - SHL Vx {, Vy}
                        default: throw new NotImplementedException($"Unknown instruction {op}");
                    }
                    break;
                // 9xy0 - SNE Vx, Vy
                case 0xA: _i = op.Addr; break; // Annn - LD I, addr
                // Bnnn - JP V0, addr
                case 0xC: _reg[op.N2] = (byte)(_rand.Next(256) & op.B2); break; // Cxkk - RND Vx, byte
                case 0xD: Draw(_reg[op.N2], _reg[op.N3], op.N4); break; // Dxyn - DRW Vx, Vy, nibble
                case 0xE:
                    switch (op.B2)
                    {
                        case 0x9E: if (IsPressed(_reg[op.N2])) { _pc += 2; } break; // Ex9E - SKP Vx
                        case 0xA1: if (!IsPressed(_reg[op.N2])) { _pc += 2; } break; // ExA1 - SKNP Vx
                        default: throw new NotImplementedException($"Unknown instruction {op}");
                    }
                    break;
                case 0xF:
                    switch(op.B2)
                    {
                        case 0x07: _reg[op.N2] = _delay; break; // Fx07 - LD Vx, DT
                        // Fx0A - LD Vx, K
                        case 0x15: _delay = _reg[op.N2]; break; // Fx15 - LD DT, Vx
                        case 0x18: _sound = _reg[op.N2]; break; // Fx18 - LD ST, Vx
                        // Fx1E - ADD I, Vx
                        case 0x29: _i = (byte)(_reg[op.N2] * 5); break; // Fx29 - LD F, Vx
                        case 0x33: Bcd(_reg[op.N2]); break; // Fx33 - LD B, Vx
                        // Fx55 - LD [I], Vx
                        case 0x65: for (int i = 0; i <= op.N2; i++) { _reg[i] = _mem[_i + i]; }; break; // Fx65 - LD Vx, [I]
                        default: throw new NotImplementedException($"Unknown instruction {op}");
                    }
                    break;
                default: throw new NotImplementedException($"Unknown instruction {op}");
            }
        }

        private bool IsPressed(byte v)
        {
            return _keys[v];
        }

        private void Bcd(byte v)
        {
            _mem[_i] = (byte)(v / 100);
            _mem[_i + 1] = (byte)(v % 100 / 10);
            _mem[_i + 2] = (byte)(v % 10);
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
                        var displayOffset = (y + dy) * DisplayWidth + x + dx;
                        if (displayOffset < _display.Length && displayOffset >=0)
                        {
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
