using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SynacoreVS19
{
    public class Synacor
    {
        private class VMState
        {
            public ushort[] Memory;
            public ushort[] Registers;
            public Stack<UInt16> stack;

            public ushort PC; // program counter

            public bool running;

            public VMState()
            {
                this.Memory = new ushort[32768]; // 15-bit addess space
                this.Registers = new ushort[8];
                this.stack = new Stack<ushort>();
                this.running = false;

            }

            public static int ToRegister(ushort number)
            {
                return number - 32768;
            }
            public static bool IsRegister(ushort number)
            {
                return number >= 32768;
            }

            public ushort getNumberOrRegister(ushort number)
            {
                ushort retVal = 0;
                if (number <= 32767)
                {
                    retVal = number;
                }
                else if (number >= 32768 && number <= 32775)
                {
                    int regNbr = ToRegister(number);
                    retVal = this.Registers[regNbr];
                }
                else
                {
                    retVal = 999; //fault
                }

                return retVal;
            }
        }

        private VMState state;


        private abstract class Opcode
        {

            public const int HALT = 0;
            public const int OUT = 19;
            public const int NOOP = 21;
            public const int ADD = 9;

            public const int JMP = 6;
            public const int JT = 7;
            public const int JF = 8;
            public const int SET = 1;
            public const int EQ = 4;
            public const int PUSH = 2;
            public const int POP = 3;
            public const int GT = 5;
            public const int AND = 12;
            public const int OR = 13;
            public const int NOT = 14;
            public const int CALL = 17;
            public const int WMEM = 16;
            public const int RMEM = 15;
            public const int RET = 18;
            public const int MULT = 10;
            public const int MOD = 11;
            public const int IN = 20;


            protected readonly VMState state;
            readonly protected ushort steps;
            abstract public void Execute();

            public Opcode(VMState s, ushort steps)
            {
                this.state = s;
                this.steps = steps;
            }

            protected static ushort clearBit16(ushort val)
            {
                ushort retVal = (ushort)(val & 0x7FFF);
                return retVal;

            }
        }

        private class Halt : Opcode
        {
            public Halt(VMState s) : base(s, 1) { }

            public override void Execute()
            {
                state.running = false;
                state.PC += steps;

            }
        }

        private class Out : Opcode
        {
            public Out(VMState s) : base(s, 2) { }

            public override void Execute()
            {
                int byteAddr = state.PC * (ushort)2;
                ushort number = state.Memory[state.PC + 1];
                ushort val = state.getNumberOrRegister(number);
                byte lower = (byte)(val & 0xFFFF);

                string s = System.Text.Encoding.ASCII.GetString(new[] { lower });
                System.Console.Write(s);
                state.PC += steps;

            }
        }

        private class Add : Opcode
        {
            public Add(VMState s) : base(s, 4) { }

            public override void Execute()
            {
                int destReg = VMState.ToRegister(state.Memory[state.PC + 1]);
                ushort numA = state.Memory[state.PC + 2];
                ushort valA = state.getNumberOrRegister(numA);

                ushort numB = state.Memory[state.PC + 3];
                ushort valB = state.getNumberOrRegister(numB);

                state.Registers[destReg] = (ushort)((valA + valB) % 32768);
                state.PC += steps;

            }
        }
        private class Mult : Opcode
        {
            public Mult(VMState s) : base(s, 4) { }

            public override void Execute()
            {
                int byteAddr = state.PC * (ushort)2;
                string s = $"counter at byte {byteAddr:x}";

                int destReg = VMState.ToRegister(state.Memory[state.PC + 1]);
                ushort numB = state.Memory[state.PC + 2];
                ushort valB = state.getNumberOrRegister(numB);

                ushort numC = state.Memory[state.PC + 3];
                ushort valC = state.getNumberOrRegister(numC);

                ushort setVal = (ushort)((valB * valC) % 32768);

                ushort numA = state.Memory[state.PC + 1];
                if (VMState.IsRegister(numA))
                {
                    int valA = VMState.ToRegister(numA);
                    state.Registers[valA] = setVal;
                }
                else
                {
                    state.Memory[numA] = setVal;
                }
                state.PC += steps;

            }
        }
        private class Mod : Opcode
        {
            public Mod(VMState s) : base(s, 4) { }

            public override void Execute()
            {
                int byteAddr = state.PC * (ushort)2;
                string s = $"counter at byte {byteAddr:x}";

                int destReg = VMState.ToRegister(state.Memory[state.PC + 1]);
                ushort numB = state.Memory[state.PC + 2];
                ushort valB = state.getNumberOrRegister(numB);

                ushort numC = state.Memory[state.PC + 3];
                ushort valC = state.getNumberOrRegister(numC);

                ushort setVal = (ushort)((valB % valC) /* % 32768*/);

                ushort numA = state.Memory[state.PC + 1];
                if (VMState.IsRegister(numA))
                {
                    int valA = VMState.ToRegister(numA);
                    state.Registers[valA] = setVal;
                }
                else
                {
                    state.Memory[numA] = setVal;
                }
                state.PC += steps;

            }
        }
        private class Noop : Opcode
        {
            public Noop(VMState s) : base(s, 1) { }

            public override void Execute()
            {
                state.PC += steps;
            }
        }

        private class Jmp : Opcode
        {
            public Jmp(VMState s) : base(s, 2) { }

            public override void Execute()
            {
                int byteAddr = state.PC * (ushort)2;
                ushort numA = state.Memory[state.PC + 1];
                ushort valA = state.getNumberOrRegister(numA);
                state.PC = valA;
            }
        }

        private class Ret : Opcode
        {
            public Ret(VMState s) : base(s, 1) { }

            public override void Execute()
            {
                int byteAddr = state.PC * (ushort)2;
                string s = $"counter at byte {byteAddr:x}";
                if (state.stack.Count == 0)
                {
                    state.running = false;
                    return;
                }
                ushort valA = state.stack.Pop();
                state.PC = valA;
            }
        }
        private class Jt : Opcode
        {
            public Jt(VMState s) : base(s, 3) { }

            public override void Execute()
            {
                int byteAddr = state.PC * (ushort)2;
                ushort numA = state.Memory[state.PC + 1];
                ushort valA = state.getNumberOrRegister(numA);
                if (valA != 0)
                {
                    ushort numB = state.Memory[state.PC + 2];
                    ushort valB = state.getNumberOrRegister(numB);
                    state.PC = valB;
                }
                else
                {
                    state.PC += steps;
                }
            }
        }
        private class Jf : Opcode
        {
            public Jf(VMState s) : base(s, 3) { }

            public override void Execute()
            {
                int byteAddr = state.PC * (ushort)2;
                ushort numA = state.Memory[state.PC + 1];
                ushort valA = state.getNumberOrRegister(numA);
                if (valA == 0)
                {
                    ushort numB = state.Memory[state.PC + 2];
                    ushort valB = state.getNumberOrRegister(numB);
                    state.PC = valB;
                }
                else
                {
                    state.PC += steps;
                }
            }
        }

        private class Set : Opcode
        {
            public Set(VMState s) : base(s, 3) { }

            public override void Execute()
            {
                int byteAddr = state.PC * (ushort)2;
                ushort numA = state.Memory[state.PC + 1];
                int valA = VMState.ToRegister(numA); /*state.getNumberOrRegister(numA)*/;

                ushort numB = state.Memory[state.PC + 2];
                ushort valB = state.getNumberOrRegister(numB);
                state.Registers[valA] = valB;

                state.PC += steps;

            }
        }

        private class Eq : Opcode
        {
            public Eq(VMState s) : base(s, 4) { }

            public override void Execute()
            {
                int byteAddr = state.PC * (ushort)2;
                string s = $"counter at byte {byteAddr:x}";

                ushort numB = state.Memory[state.PC + 2];
                ushort valB = state.getNumberOrRegister(numB);

                ushort numC = state.Memory[state.PC + 3];
                ushort valC = state.getNumberOrRegister(numC);
                ushort setVal = (ushort)((valB == valC) ? 1 : 0);

                ushort numA = state.Memory[state.PC + 1];
                if (VMState.IsRegister(numA))
                {
                    int valA = VMState.ToRegister(numA);
                    state.Registers[valA] = setVal;
                }
                else
                {
                    state.Memory[numA] = setVal;
                }
                state.PC += steps;

            }
        }
        private class Gt : Opcode
        {
            public Gt(VMState s) : base(s, 4) { }

            public override void Execute()
            {
                int byteAddr = state.PC * (ushort)2;
                string s = $"counter at byte {byteAddr:x}";

                ushort numB = state.Memory[state.PC + 2];
                ushort valB = state.getNumberOrRegister(numB);

                ushort numC = state.Memory[state.PC + 3];
                ushort valC = state.getNumberOrRegister(numC);
                ushort setVal = (ushort)((valB > valC) ? 1 : 0);

                ushort numA = state.Memory[state.PC + 1];
                if (VMState.IsRegister(numA))
                {
                    int valA = VMState.ToRegister(numA);
                    state.Registers[valA] = setVal;
                }
                else
                {
                    state.Memory[numA] = setVal;
                }
                state.PC += steps;

            }
        }

        private class And : Opcode
        {
            public And(VMState s) : base(s, 4) { }

            public override void Execute()
            {
                int byteAddr = state.PC * (ushort)2;
                string s = $"counter at byte {byteAddr:x}";

                ushort numB = state.Memory[state.PC + 2];
                ushort valB = state.getNumberOrRegister(numB);

                ushort numC = state.Memory[state.PC + 3];
                ushort valC = state.getNumberOrRegister(numC);

                ushort setVal = (ushort)(valB & valC);

                ushort numA = state.Memory[state.PC + 1];
                if (VMState.IsRegister(numA))
                {
                    int valA = VMState.ToRegister(numA);
                    state.Registers[valA] = setVal;
                }
                else
                {
                    state.Memory[numA] = setVal;
                }
                state.PC += steps;

            }
        }
        private class Or : Opcode
        {
            public Or(VMState s) : base(s, 4) { }

            public override void Execute()
            {
                int byteAddr = state.PC * (ushort)2;
                string s = $"counter at byte {byteAddr:x}";

                ushort numB = state.Memory[state.PC + 2];
                ushort valB = state.getNumberOrRegister(numB);

                ushort numC = state.Memory[state.PC + 3];
                ushort valC = state.getNumberOrRegister(numC);

                ushort setVal = (ushort)(valB | valC);

                ushort numA = state.Memory[state.PC + 1];
                if (VMState.IsRegister(numA))
                {
                    int valA = VMState.ToRegister(numA);
                    state.Registers[valA] = setVal;
                }
                else
                {
                    state.Memory[numA] = setVal;
                }
                state.PC += steps;

            }
        }

        private class Not : Opcode
        {
            public Not(VMState s) : base(s, 3) { }

            public override void Execute()
            {
                int byteAddr = state.PC * (ushort)2;
                string s = $"counter at byte {byteAddr:x}";

                ushort numB = state.Memory[state.PC + 2];
                ushort valB = state.getNumberOrRegister(numB);

                ushort setVal = clearBit16((ushort)~valB);

                ushort numA = state.Memory[state.PC + 1];
                if (VMState.IsRegister(numA))
                {
                    int valA = VMState.ToRegister(numA);
                    state.Registers[valA] = setVal;
                }
                else
                {
                    state.Memory[numA] = setVal;
                }
                state.PC += steps;

            }
        }


        private class Push : Opcode
        {
            public Push(VMState s) : base(s, 2) { }

            public override void Execute()
            {
                int byteAddr = state.PC * (ushort)2;
                string s = $"counter at byte {byteAddr:x}";

                ushort numA = state.Memory[state.PC + 1];
                ushort valA = state.getNumberOrRegister(numA);

                state.stack.Push(valA);
                state.PC += steps;

            }
        }

        private class Pop : Opcode
        {
            public Pop(VMState s) : base(s, 2) { }

            public override void Execute()
            {
                int byteAddr = state.PC * (ushort)2;
                string s = $"counter at byte {byteAddr:x}";

                ushort top = state.stack.Pop();
                ushort numA = state.Memory[state.PC + 1];

                if (VMState.IsRegister(numA))
                {
                    int valA = VMState.ToRegister(numA);
                    state.Registers[valA] = top;
                }
                else
                {
                    state.Memory[numA] = top;
                }

                state.PC += steps;
            }
        }
        private class Call : Opcode
        {
            public Call(VMState s) : base(s, 2) { }

            public override void Execute()
            {
                int byteAddr = state.PC * (ushort)2;
                string s = $"counter at byte {byteAddr:x}";

                ushort numA = state.Memory[state.PC + 1];
                ushort valA = state.getNumberOrRegister(numA);

                ushort nextInstr = (ushort)(state.PC + steps);

                state.stack.Push(nextInstr);
                state.PC = valA;


            }
        }
        private class Rmem : Opcode
        {
            public Rmem(VMState s) : base(s, 3) { }

            public override void Execute()
            {
                int byteAddr = state.PC * (ushort)2;
                string s = $"counter at byte {byteAddr:x}";

                ushort numA = state.Memory[state.PC + 1];

                ushort numB = state.Memory[state.PC + 2];
                ushort valB;
                if (VMState.IsRegister(numB))
                {
                    ushort reg = (ushort)VMState.ToRegister(numB);
                    ushort addr = state.Registers[reg];
                    valB = state.Memory[addr];
                }
                else
                {
                    valB = state.Memory[numB];
                }

                if (VMState.IsRegister(numA))
                {
                    int valA = VMState.ToRegister(numA);
                    state.Registers[valA] = valB;
                }
                else
                {
                    state.Memory[numA] = valB;
                }

                state.PC += steps;

            }
        }
        private class Wmem : Opcode
        {
            public Wmem(VMState s) : base(s, 3) { }

            public override void Execute()
            {
                int byteAddr = state.PC * (ushort)2;
                string s = $"counter at byte {byteAddr:x}";

                ushort numA = state.Memory[state.PC + 1];

                ushort numB = state.Memory[state.PC + 2];
                ushort valB = state.getNumberOrRegister(numB);

                if (VMState.IsRegister(numA))
                {
                    ushort reg = (ushort)VMState.ToRegister(numA);
                    ushort addr = state.Registers[reg];
                    state.Memory[addr] = valB;
                }
                else
                {
                    state.Memory[numA] = valB;
                }

                state.PC += steps;

            }
        }


        private class In : Opcode
        {
            public In(VMState s) : base(s, 2) { }

            public override void Execute()
            {
                int byteAddr = state.PC * (ushort)2;
                string s = $"counter at byte {byteAddr:x}";

                ushort numA = state.Memory[state.PC + 1];


                int c = System.Console.Read();
                if (c == 13)
                {
                    c = System.Console.Read();  // skip past \r
                }
                char x = (char)c;
                byte b = Convert.ToByte(c);
                ushort ch = (ushort)b;
                if (VMState.IsRegister(numA))
                {
                    int valA = VMState.ToRegister(numA);
                    state.Registers[valA] = ch;
                }
                else
                {
                    state.Memory[numA] = ch;
                }

                state.PC += steps;

            }
        }


        private Dictionary<ushort, Opcode> Opcodes = new Dictionary<ushort, Opcode>();

        public Synacor()
        {
            state = new VMState();
            Opcodes[Opcode.HALT] = new Halt(state);
            Opcodes[Opcode.OUT] = new Out(state);
            Opcodes[Opcode.ADD] = new Add(state);
            Opcodes[Opcode.NOOP] = new Noop(state);
            Opcodes[Opcode.JMP] = new Jmp(state);
            Opcodes[Opcode.JT] = new Jt(state);
            Opcodes[Opcode.JF] = new Jf(state);
            Opcodes[Opcode.SET] = new Set(state);
            Opcodes[Opcode.EQ] = new Eq(state);
            Opcodes[Opcode.GT] = new Gt(state);
            Opcodes[Opcode.PUSH] = new Push(state);
            Opcodes[Opcode.POP] = new Pop(state);
            Opcodes[Opcode.AND] = new And(state);
            Opcodes[Opcode.OR] = new Or(state);
            Opcodes[Opcode.NOT] = new Not(state);
            Opcodes[Opcode.CALL] = new Call(state);
            Opcodes[Opcode.MULT] = new Mult(state);
            Opcodes[Opcode.MOD] = new Mod(state);
            Opcodes[Opcode.RMEM] = new Rmem(state);
            Opcodes[Opcode.WMEM] = new Wmem(state);
            Opcodes[Opcode.RET] = new Ret(state);
            Opcodes[Opcode.IN] = new In(state);

        }
        public void Load()
        {
            ushort[] sampleProgram = new ushort[] { 9, 32768, 32769, 87, 19, 32768 };

            ushort counter = 0;
            // foreach(ushort word in sampleProgram)
            // {
            //     state.Memory[counter] = word;
            //     counter++;

            // }

            string fileName = @"./challenge.bin";
            using (BinaryReader reader = new BinaryReader(File.Open(fileName, FileMode.Open)))
            {
                int pos = 0;

                int length = (int)reader.BaseStream.Length;
                while (pos < length)
                {
                    ushort number = reader.ReadUInt16();
                    state.Memory[counter] = number;
                    counter++;
                    pos += sizeof(ushort);
                }
            }
        }

        public void Go()
        {
            state.PC = 0;

            state.running = true;
            while (state.running)
            {
                ushort instruction = state.Memory[state.PC];
                if (Opcodes.ContainsKey(instruction))
                {
                    Opcodes[instruction].Execute();
                }
                else
                {
                    state.running = false;       // fault;
                    var errMSg = $"FAULT! invalid opcode at PC {state.PC:x} (byte ({state.PC * 2:x}))";
                    System.Console.Write(errMSg);
                }
            }
        }
    }
}
