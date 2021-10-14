using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using CryptoPrivacy;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Microsoft.CSharp;
using System.Diagnostics;
using System.Threading;

namespace NetShield_Protector
{
	// Token: 0x02000002 RID: 2
	public partial class Main : Form
	{
        private readonly string[] FakeObfuscastionsAttributes = { "ConfusedByAttribute", "YanoAttribute", "NetGuard", "DotfuscatorAttribute", "BabelAttribute", "ObfuscatedByGoliath", "dotNetProtector" };

        RainbowObj rgb = null, rgb2 = null, rgb3 = null;
        public ModuleDefMD asm;
        public AssemblyDef asmDef;
        public string asmPath;

		public Main() { this.InitializeComponent(); }

		private string RandomPassword(int PasswordLength)
		{
			StringBuilder stringBuilder = new StringBuilder();
			Random random = new Random();
			while (0 < PasswordLength--)
			{
				string text = "ABCDEFGHIJKLMNOPQRSTUVWXYZ*!@=&?&/abcdefghijklmnopqrstuvwxyz1234567890";
				stringBuilder.Append(text[random.Next(text.Length)]);
			}
			return stringBuilder.ToString();
		}
		private string RandomName(int NameLength)
		{
			StringBuilder stringBuilder = new StringBuilder();
			Random random = new Random();
			while (0 < NameLength--)
			{
				string text = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
				stringBuilder.Append(text[random.Next(text.Length)]);
			}
			return stringBuilder.ToString();
		}

        #region Protections - Obfuscation
        public class Block
        {
            public Block() { this.Instructions = new List<Instruction>(); }
            public List<Instruction> Instructions { get; set; }
            public int Number { get; set; }
        }
        public static List<Block> GetMethod(MethodDef method)
        {
            var blocks = new List<Block>();
            var block = new Block();
            int stacks = 0, pops = 0, usage = 0, id = 0;
            block.Number = id;
            block.Instructions.Add(Instruction.Create(OpCodes.Nop));
            blocks.Add(block);
            block = new Block();
            var handlers = new Stack<ExceptionHandler>();
            foreach (var instruction in method.Body.Instructions)
            {
                foreach (var eh in method.Body.ExceptionHandlers) { if (eh.HandlerStart == instruction || eh.TryStart == instruction || eh.FilterStart == instruction) handlers.Push(eh); }
                foreach (var eh in method.Body.ExceptionHandlers) { if (eh.HandlerEnd == instruction || eh.TryEnd == instruction) handlers.Pop(); }
                instruction.CalculateStackUsage(out stacks, out pops);
                block.Instructions.Add(instruction); usage += stacks - pops;
                if (stacks == 0)
                {
                    if (instruction.OpCode != OpCodes.Nop)
                    {
                        if ((usage == 0 || instruction.OpCode == OpCodes.Ret) && handlers.Count == 0)
                        {
                            block.Number = ++id;
                            blocks.Add(block);
                            block = new Block();
                        }
                    }
                }
            }
            return blocks;
        }

        private void CreateAntiDe4dot()
        {
            for (int i = 0; i < 100; i++)
            {
                InterfaceImpl Interface = new InterfaceImplUser(asm.GlobalType);
                TypeDef typedef = new TypeDefUser("", "Form" + i, asm.CorLibTypes.GetTypeRef("System", "Attribute"));
                InterfaceImpl interface1 = new InterfaceImplUser(typedef);
                asm.Types.Add(typedef);
                typedef.Interfaces.Add(interface1);
                typedef.Interfaces.Add(Interface);
            }
        }
        private void AddRenaming()
        {
            foreach (TypeDef type in asm.Types)
            {
                asm.Name = RandomName(12);
                if (type.IsGlobalModuleType || type.IsRuntimeSpecialName || type.IsSpecialName || type.IsWindowsRuntime || type.IsInterface) { continue; }
                else
                {
                    for (int i = 0; i < 100; i++)
                    {
                        foreach (PropertyDef property in type.Properties)
                        {
                            if (property.IsRuntimeSpecialName) continue;
                            property.Name = RandomName(20) + i + RandomName(10) + i;
                        }
                        foreach (FieldDef fields in type.Fields) { fields.Name = RandomName(20) + i + RandomName(10) + i; }
                        foreach (EventDef eventdef in type.Events) { eventdef.Name = RandomName(20) + i + RandomName(10) + i; }
                        foreach (MethodDef method in type.Methods) // check when debugging
                        {
                            if (method.IsConstructor || method.IsRuntimeSpecialName || method.IsRuntime || method.IsStaticConstructor || method.IsVirtual) continue;
                            foreach(Parameter RenameParameters in method.Parameters) { RenameParameters.Name = RandomName(10); }
                            method.Name = RandomName(20) + i + RandomName(10) + i;
                        }
                    }
                }
                foreach (ModuleDefMD module in asm.Assembly.Modules) { module.Name = RandomName(13); module.Assembly.Name = RandomName(14); }
            }

            /*foreach (TypeDef type in asm.Types)
            {
                foreach (MethodDef GetMethods in type.Methods)
                {
                    for (int i = 0; i < 100; i++)
                    {
                        if (GetMethods.IsConstructor || GetMethods.IsRuntimeSpecialName || GetMethods.IsRuntime || GetMethods.IsStaticConstructor) continue;
                        GetMethods.Name = RandomName(15) + i;
                    }
                }
            }*/
        }
        private void FakeAttribs() { foreach (string fakeAttrib in FakeObfuscastionsAttributes) { asm.Types.Add(new TypeDefUser(fakeAttrib, asm.CorLibTypes.Object.TypeDefOrRef)); } }
        private void CreateNamespaceJunk()
        {
            for (int i = 0; i < 200; i++)
            {
                /* pretty useless -> */ asm.Types.Add(new TypeDefUser(RandomName(10) + i + RandomName(10) + i + RandomName(10) + i, asm.CorLibTypes.Object.TypeDefOrRef));
                asm.Types.Add(new TypeDefUser("<" + RandomName(10) + i + RandomName(10) + i + RandomName(10) + i + ">", asm.CorLibTypes.Object.TypeDefOrRef));
                asm.Types.Add(new TypeDefUser(RandomName(11) + i + RandomName(11) + i + RandomName(11) + i, asm.CorLibTypes.Object.TypeDefOrRef));
                asm.Types.Add(new TypeDefUser("<" + RandomName(10) + i + RandomName(10) + i + RandomName(10) + i + ">", asm.CorLibTypes.Object.Namespace));
                asm.Types.Add(new TypeDefUser(RandomName(11) + i + RandomName(11) + i + RandomName(11) + i, asm.CorLibTypes.Object.Namespace));
            }
        }
        private void EncodeStrings()
        {
            foreach (TypeDef type in asm.Types)
            {
                foreach (MethodDef method in type.Methods)
                {
                    if (method.Body == null) continue;
                    method.Body.SimplifyBranches();
                    for (int i = 0; i < method.Body.Instructions.Count; i++)
                    {
                        if (method.Body.Instructions[i].OpCode == OpCodes.Ldstr)
                        {
                            string EncodedString = method.Body.Instructions[i].Operand.ToString();
                            string InsertEncodedString = Convert.ToBase64String(UTF8Encoding.UTF8.GetBytes(EncodedString));
                            method.Body.Instructions[i].OpCode = OpCodes.Nop;
                            method.Body.Instructions.Insert(i + 1, new Instruction(OpCodes.Call, asm.Import(typeof(Encoding).GetMethod("get_UTF8", new Type[] { }))));
                            method.Body.Instructions.Insert(i + 2, new Instruction(OpCodes.Ldstr, InsertEncodedString));
                            method.Body.Instructions.Insert(i + 3, new Instruction(OpCodes.Call, asm.Import(typeof(Convert).GetMethod("FromBase64String", new Type[] { typeof(string) }))));
                            method.Body.Instructions.Insert(i + 4, new Instruction(OpCodes.Callvirt, asm.Import(typeof(Encoding).GetMethod("GetString", new Type[] { typeof(byte[]) }))));
                            i += 4;
                        }
                    }
                }
            }
        }
        private void ObfuscateCFlow()
        {
            foreach (var tDef in asm.Types)
            {
                if (tDef == asm.GlobalType) continue;
                foreach (var mDef in tDef.Methods)
                {
                    if (mDef.Name.StartsWith("get_") || mDef.Name.StartsWith("set_")) continue;
                    if (!mDef.HasBody || mDef.IsConstructor) continue;
                    mDef.Body.SimplifyBranches();
                    mDef.Body.SimplifyMacros(mDef.Parameters);
                    var blocks = GetMethod(mDef);
                    var ret = new List<Block>();
                    foreach (var group in blocks)
                    {
                        Random rnd = new Random();
                        ret.Insert(rnd.Next(0, ret.Count), group);
                    }
                    blocks = ret;
                    mDef.Body.Instructions.Clear();
                    var local = new Local(mDef.Module.CorLibTypes.Int32);
                    mDef.Body.Variables.Add(local);
                    var target = Instruction.Create(OpCodes.Nop);
                    var instr = Instruction.Create(OpCodes.Br, target);
                    var instructions = new List<Instruction> { Instruction.Create(OpCodes.Ldc_I4, 0) };
                    foreach (var instruction in instructions)
                        mDef.Body.Instructions.Add(instruction);
                    mDef.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc, local));
                    mDef.Body.Instructions.Add(Instruction.Create(OpCodes.Br, instr));
                    mDef.Body.Instructions.Add(target);
                    foreach (var block in blocks.Where(block => block != blocks.Single(x => x.Number == blocks.Count - 1)))
                    {
                        mDef.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc, local));
                        var instructions1 = new List<Instruction> { Instruction.Create(OpCodes.Ldc_I4, block.Number) };
                        foreach (var instruction in instructions1)
                            mDef.Body.Instructions.Add(instruction);
                        mDef.Body.Instructions.Add(Instruction.Create(OpCodes.Ceq));
                        var instruction4 = Instruction.Create(OpCodes.Nop);
                        mDef.Body.Instructions.Add(Instruction.Create(OpCodes.Brfalse, instruction4));

                        foreach (var instruction in block.Instructions)
                            mDef.Body.Instructions.Add(instruction);

                        var instructions2 = new List<Instruction> { Instruction.Create(OpCodes.Ldc_I4, block.Number + 1) };
                        foreach (var instruction in instructions2)
                            mDef.Body.Instructions.Add(instruction);

                        mDef.Body.Instructions.Add(Instruction.Create(OpCodes.Stloc, local));
                        mDef.Body.Instructions.Add(instruction4);
                    }
                    mDef.Body.Instructions.Add(Instruction.Create(OpCodes.Ldloc, local));
                    var instructions3 = new List<Instruction> { Instruction.Create(OpCodes.Ldc_I4, blocks.Count - 1) };
                    foreach (var instruction in instructions3)
                        mDef.Body.Instructions.Add(instruction);
                    mDef.Body.Instructions.Add(Instruction.Create(OpCodes.Ceq));
                    mDef.Body.Instructions.Add(Instruction.Create(OpCodes.Brfalse, instr));
                    mDef.Body.Instructions.Add(Instruction.Create(OpCodes.Br, blocks.Single(x => x.Number == blocks.Count - 1).Instructions[0]));
                    mDef.Body.Instructions.Add(instr);

                    foreach (var lastBlock in blocks.Single(x => x.Number == blocks.Count - 1).Instructions)
                        mDef.Body.Instructions.Add(lastBlock);
                }
            }
        }
        private void ConfuseIntegers()
        {
            foreach (var type in asm.GetTypes())
            {
                if (type.IsGlobalModuleType) continue;
                foreach (var method in type.Methods)
                {
                    if (!method.HasBody) continue;
                    {
                        for (var i = 0; i < method.Body.Instructions.Count; i++)
                        {
                            if (!method.Body.Instructions[i].IsLdcI4()) continue;
                            var numorig = new Random(Guid.NewGuid().GetHashCode()).Next();
                            var div = new Random(Guid.NewGuid().GetHashCode()).Next();
                            var num = numorig ^ div;
                            var nop = OpCodes.Nop.ToInstruction();
                            var local = new Local(method.Module.ImportAsTypeSig(typeof(int)));
                            method.Body.Variables.Add(local);
                            method.Body.Instructions.Insert(i + 1, OpCodes.Stloc.ToInstruction(local));
                            method.Body.Instructions.Insert(i + 2, Instruction.Create(OpCodes.Ldc_I4, method.Body.Instructions[i].GetLdcI4Value() - sizeof(float)));
                            method.Body.Instructions.Insert(i + 3, Instruction.Create(OpCodes.Ldc_I4, num));
                            method.Body.Instructions.Insert(i + 4, Instruction.Create(OpCodes.Ldc_I4, div));
                            method.Body.Instructions.Insert(i + 5, Instruction.Create(OpCodes.Xor));
                            method.Body.Instructions.Insert(i + 6, Instruction.Create(OpCodes.Ldc_I4, numorig));
                            method.Body.Instructions.Insert(i + 7, Instruction.Create(OpCodes.Bne_Un, nop));
                            method.Body.Instructions.Insert(i + 8, Instruction.Create(OpCodes.Ldc_I4, 2));
                            method.Body.Instructions.Insert(i + 9, OpCodes.Stloc.ToInstruction(local));
                            method.Body.Instructions.Insert(i + 10, Instruction.Create(OpCodes.Sizeof, method.Module.Import(typeof(float))));
                            method.Body.Instructions.Insert(i + 11, Instruction.Create(OpCodes.Add));
                            method.Body.Instructions.Insert(i + 12, nop);
                            i += 12;
                        }
                        method.Body.SimplifyBranches();
                    }
                }
            }
        }
        private void AddILDasm()
        {
            foreach (ModuleDef module in asm.Assembly.Modules)
            {
                TypeRef attrRef = asm.CorLibTypes.GetTypeRef("System.Runtime.CompilerServices", "SuppressIldasmAttribute");
                var ctorRef = new MemberRefUser(module, ".ctor", MethodSig.CreateInstance(module.CorLibTypes.Void), attrRef);
                var attr = new CustomAttribute(ctorRef);
                module.CustomAttributes.Add(attr);
            }
        }
        private void ConvertCalls()
        {
            foreach (TypeDef type in asm.Types)
            {
                foreach (MethodDef method in type.Methods)
                {
                    if (method.IsConstructor && method.DeclaringType.IsGlobalModuleType)
                    {
                        for (int i = 0; i < method.Body.Instructions.Count - 1; i++)
                        {
                            if (method.Body.Instructions[i].OpCode == OpCodes.Call || method.Body.Instructions[i].OpCode == OpCodes.Callvirt && method.Body.Instructions[i].ToString().Contains("ISupportInitialize"))
                            {
                                MemberRef membertocalli = (MemberRef)method.Body.Instructions[i].Operand;
                                method.Body.Instructions[i].OpCode = OpCodes.Calli;
                                method.Body.Instructions[i].Operand = membertocalli.MethodSig;
                                method.Body.Instructions.Insert(i, Instruction.Create(OpCodes.Ldftn, membertocalli));
                            }
                        }
                    }
                }
            }
        }
        #endregion

        private void ObfuscasteCode()
        {
            if (checkBox2.Checked) { CreateAntiDe4dot(); }
            if (checkBox5.Checked) { FakeAttribs(); }
            if (checkBox8.Checked) { AddRenaming(); }
            if (checkBox6.Checked) { CreateNamespaceJunk(); }
            if (checkBox1.Checked) { EncodeStrings(); }
            if (checkBox7.Checked) { ObfuscateCFlow(); }
            if (checkBox10.Checked) { ConfuseIntegers(); }
            if (checkBox11.Checked) { AddILDasm(); }
            if (checkBox13.Checked) { ConvertCalls(); }

            asm.Write(Environment.CurrentDirectory + @"\Obfuscated.exe");
            if (checkBox3.Checked)
            {
                string RandomAssemblyName = RandomName(12);
                PackAndEncrypt(Environment.CurrentDirectory + @"\Obfuscated.exe", Environment.CurrentDirectory + @"\" + RandomAssemblyName + ".tmp");
                File.Delete(Environment.CurrentDirectory + @"\Obfuscated.exe");
                File.Move(Environment.CurrentDirectory + @"\" + RandomAssemblyName + ".tmp", Environment.CurrentDirectory + @"\Obfuscated.exe");
            }
        }

        private string XOREncryptionKeys(string KeyToEncrypt, string Key)
        {
            StringBuilder stringBuilder = new StringBuilder();
            for (int i = 0; i < KeyToEncrypt.Length; i++)
            {
                stringBuilder.Append(KeyToEncrypt[i] ^ Key[i % 4]);
            }
            return stringBuilder.ToString();
        }
        private void PackAndEncrypt(string FileToPack, string Output)
        {
            var Options = new Dictionary<string, string>();
            Options.Add("CompilerVersion", "v4.0");
            Options.Add("language", "c#");
            var codeProvider = new CSharpCodeProvider(Options);
            CompilerParameters parameters = new CompilerParameters();
            parameters.CompilerOptions = "/target:winexe /unsafe";
            parameters.GenerateExecutable = true;
            parameters.OutputAssembly = Output;
            parameters.IncludeDebugInformation = false;
            parameters.TreatWarningsAsErrors = false;
            string[] Librarys = { "System", "System.Windows.Forms", "System.Management", "System.Net", "System.Core", "System.Net.Http", "System.Runtime", "System.Runtime.InteropServices" };
            foreach (string Library in Librarys) { parameters.ReferencedAssemblies.Add(Library + ".dll"); }
            byte[] CodeToProtect = File.ReadAllBytes(FileToPack);
            string RandomIV = RandomName(16);
            string RandomKey = RandomPassword(17);
            string RandomXORKey = RandomPassword(4);
            string EncryptedKey = XOREncryptionKeys(RandomKey, RandomXORKey);
            string EncryptedIV = XOREncryptionKeys(RandomIV, RandomXORKey);
            AesAlgorithms EncryptingBytes = new AesAlgorithms();
            string Final = EncryptingBytes.AesTextEncryption(Convert.ToBase64String(CodeToProtect).Replace("A", ".").Replace("B", "*").Replace("S", @"_"), EncryptedKey, EncryptedIV);
            string PackStub = Properties.Resources.PackStub;
            string NewPackStub = PackStub.Replace("DecME", Final).Replace("THISISIV", RandomIV).Replace("THISISKEY", RandomKey);
            string TotallyNewPackStub = NewPackStub.Replace("decryptkeyencryption", Convert.ToBase64String(Encoding.UTF8.GetBytes(RandomXORKey))).Replace("decryptkeyiv", Convert.ToBase64String(Encoding.UTF8.GetBytes(RandomXORKey))).Replace("PackStub", "namespace " + RandomName(12));
            CompilerResults cr = codeProvider.CompileAssemblyFromSource(parameters, TotallyNewPackStub);
            if (cr.Errors.Count > 0)
            {
                foreach (CompilerError ce in cr.Errors)
                {
                    MessageBox.Show("Errors building: " + ce.ErrorText + ", in line: " + ce.Line);
                }
            }
        }

        private string GetHardwareID()
        {
            textBox2.Text = "Calculating HWID...";
            ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
            ManagementObjectCollection managementObjectCollection = managementObjectSearcher.Get();
            string str = null;
            foreach (ManagementBaseObject managementBaseObject in managementObjectCollection)
            {
                ManagementObject managementObject = (ManagementObject)managementBaseObject;
                str = managementObject["ProcessorType"].ToString() + managementObject["ProcessorId"].ToString();
            }
            ManagementObjectSearcher managementObjectSearcher2 = new ManagementObjectSearcher("SELECT * FROM Win32_BIOS");
            ManagementObjectCollection managementObjectCollection2 = managementObjectSearcher2.Get();
            string str2 = null;
            foreach (ManagementBaseObject managementBaseObject2 in managementObjectCollection2)
            {
                ManagementObject managementObject2 = (ManagementObject)managementBaseObject2;
                str2 = managementObject2["Manufacturer"].ToString() + managementObject2["Version"].ToString();
            }
            return Main.HashingHardwareID(str + str2);
        }
		private static string HashingHardwareID(string ToHash)
		{
			byte[] bytes = Encoding.ASCII.GetBytes("bAI!J6XwWO&A");
			byte[] array = new HMACSHA256
			{
				Key = bytes
			}.ComputeHash(Encoding.UTF8.GetBytes(ToHash));
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < array.Length; i++)
			{
				stringBuilder.Append(array[i].ToString("x2"));
			}
			return stringBuilder.ToString();
		}
		private static string GetUSBHardwareID(string USBPath)
		{
			DriveInfo[] drives = DriveInfo.GetDrives();
			foreach (DriveInfo driveInfo in drives)
			{
				bool flag = driveInfo.RootDirectory.ToString() == USBPath;
				if (flag)
				{
					ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");
					ManagementObjectCollection managementObjectCollection = managementObjectSearcher.Get();
					foreach (ManagementBaseObject managementBaseObject in managementObjectCollection)
					{
						ManagementObject managementObject = (ManagementObject)managementBaseObject;
						bool flag2 = managementObject["MediaType"].ToString() == "Removable Media";
						if (flag2)
						{
							return Main.HashingHardwareID(driveInfo.TotalSize.ToString() + managementObject["SerialNumber"].ToString() + managementObject["PNPDeviceID"].ToString());
						}
					}
				}
			}
			return null;
		}

        private void HWIDPacking(string Output)
        {
            var Options = new Dictionary<string, string>();
            Options.Add("CompilerVersion", "v4.0");
            Options.Add("language", "c#");
            var codeProvider = new CSharpCodeProvider(Options);
            CompilerParameters parameters = new CompilerParameters();
            parameters.CompilerOptions = "/target:winexe";
            parameters.GenerateExecutable = true;
            parameters.OutputAssembly = Output;
            parameters.IncludeDebugInformation = false;
            string[] Librarys = { "System", "System.Windows.Forms", "System.Management", "System.Core", "System.Runtime", "System.Runtime.InteropServices" };
            foreach (string Library in Librarys)
            {
                parameters.ReferencedAssemblies.Add(Library + ".dll");
            }
            byte[] CodeToProtect = File.ReadAllBytes(asmPath);
            string RandomIV = RandomPassword(16);
            string RandomKey = RandomPassword(4);
            StringBuilder ROT13Encoding = new StringBuilder();
            Regex regex = new Regex("[A-Za-z]");
            foreach (char KSXZ in XOREncryptionKeys(textBox2.Text, RandomKey))
            {
                if (regex.IsMatch(KSXZ.ToString()))
                {
                    int C = ((KSXZ & 223) - 52) % 26 + (KSXZ & 32) + 65;
                    ROT13Encoding.Append((char)C);
                }
            }
            AesAlgorithms EncryptingBytes = new AesAlgorithms();
            string Final = EncryptingBytes.AesTextEncryption(Convert.ToBase64String(CodeToProtect), ROT13Encoding.ToString(), RandomIV);
            string HWIDPacker = Properties.Resources.HWIDPacker;
            string NewHWIDPackerCode = HWIDPacker.Replace("DecME", Final).Replace("THISISIV", RandomIV).Replace("HWIDPacker", "namespace " + RandomName(14));
            string MyShinyNewPacker = NewHWIDPackerCode.Replace("SOS12", Convert.ToBase64String(Encoding.UTF8.GetBytes(RandomKey)));
            codeProvider.CompileAssemblyFromSource(parameters, MyShinyNewPacker);
        }
        private void LicensePacking(string Output)
        {
            var Options = new Dictionary<string, string>();
            Options.Add("CompilerVersion", "v4.0");
            Options.Add("language", "c#");
            var codeProvider = new CSharpCodeProvider(Options);
            CompilerParameters parameters = new CompilerParameters();
            parameters.CompilerOptions = "/target:winexe";
            parameters.GenerateExecutable = true;
            parameters.OutputAssembly = Output;
            parameters.IncludeDebugInformation = false;
            string[] Librarys = { "System", "System.Windows.Forms", "System.Management", "System.Net", "System.Core", "System.Net.Http", "System.Runtime", "System.Runtime.InteropServices" };
            foreach (string Library in Librarys)
            {
                parameters.ReferencedAssemblies.Add(Library + ".dll");
            }
            byte[] CodeToProtect = File.ReadAllBytes(asmPath);
            string RandomIV = RandomPassword(16);
            AesAlgorithms EncryptingBytes = new AesAlgorithms();
            string RandomKey = RandomPassword(4);
            StringBuilder ROT13Encoding = new StringBuilder();
            Regex regex = new Regex("[A-Za-z]");
            foreach (char KSXZ in XOREncryptionKeys(textBox3.Text, RandomKey))
            {
                if (regex.IsMatch(KSXZ.ToString()))
                {
                    int C = ((KSXZ & 223) - 52) % 26 + (KSXZ & 32) + 65;
                    ROT13Encoding.Append((char)C);
                }
            }
            string Final = EncryptingBytes.AesTextEncryption(Convert.ToBase64String(CodeToProtect), ROT13Encoding.ToString(), RandomIV);
            string LicensePacker = Properties.Resources.LicensePacker;
            string NewLicensePackerCode = LicensePacker.Replace("DecME", Final).Replace("THISISIV", RandomIV).Replace("LicensePacker", "namespace " + RandomName(14));
            string MyShinyNewPacker = NewLicensePackerCode.Replace("decryptkeyencryption", Convert.ToBase64String(Encoding.UTF8.GetBytes(RandomKey))).Replace("SOS13", textBox4.Text);
            codeProvider.CompileAssemblyFromSource(parameters, MyShinyNewPacker);
        }
        private void USBPacking(string Output)
        {
            var Options = new Dictionary<string, string>();
            Options.Add("CompilerVersion", "v4.0");
            Options.Add("language", "c#");
            var codeProvider = new CSharpCodeProvider(Options);
            CompilerParameters parameters = new CompilerParameters();
            parameters.CompilerOptions = "/target:winexe";
            parameters.GenerateExecutable = true;
            parameters.OutputAssembly = Output;
            parameters.IncludeDebugInformation = false;
            string[] Librarys = { "System", "System.Windows.Forms", "System.Management", "System.Net", "System.Core", "System.Net.Http", "System.Runtime", "System.Runtime.InteropServices" };
            foreach (string Library in Librarys)
            {
                parameters.ReferencedAssemblies.Add(Library + ".dll");
            }
            byte[] CodeToProtect = File.ReadAllBytes(asmPath);
            string RandomIV = RandomPassword(16);
            AesAlgorithms EncryptingBytes = new AesAlgorithms();
            string RandomKey = RandomPassword(4);
            string USBHWID = GetUSBHardwareID(comboBox1.Text);
            string EncryptedKey = XOREncryptionKeys(USBHWID, RandomKey);
            string Final = EncryptingBytes.AesTextEncryption(Convert.ToBase64String(CodeToProtect), Convert.ToBase64String(Encoding.UTF8.GetBytes(EncryptedKey)), RandomIV);
            string USBPacker = Properties.Resources.USBPacker;
            string NewUSBPackerCode = USBPacker.Replace("DecME", Final).Replace(GetUSBHardwareID(comboBox1.Text), USBPacker).Replace("THISISIV", RandomIV).Replace("USBPacker", "namespace " + RandomName(14)).Replace("USBSADASAS", HashingHardwareID(comboBox1.Text));
            string MyShinyNewPacker = NewUSBPackerCode.Replace("SOS12", Convert.ToBase64String(UTF8Encoding.UTF8.GetBytes(RandomKey)));
            codeProvider.CompileAssemblyFromSource(parameters, MyShinyNewPacker);
        }

		// Token: 0x0600000E RID: 14 RVA: 0x000049BC File Offset: 0x00002BBC
		private void button3_Click(object sender, EventArgs e)
		{
			try
			{
                bool isObf = false;
                if (this.checkBox1.Checked || this.checkBox2.Checked || this.checkBox5.Checked || this.checkBox6.Checked || this.checkBox7.Checked || this.checkBox8.Checked || this.checkBox10.Checked || this.checkBox11.Checked || this.checkBox13.Checked)
                {
                    isObf = true;
                    ObfuscasteCode();
                }
                if (checkBox3.Checked)
                {
                    asm = ModuleDefMD.Load(Environment.CurrentDirectory + "\\Obfuscated.exe");
                    asmPath = Environment.CurrentDirectory + "\\Obfuscated.exe";
                    if (radioButton1.Checked)
                    {
                        if (string.IsNullOrEmpty(this.textBox2.Text))
                        {
                            if (MessageBox.Show("No HWID Entered, use Computer HWID Instead?", "Error", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Asterisk) == DialogResult.OK) { button2_Click(null, null); }
                            else { return; }
                        }
                        else
                        {
                            if (this.checkBox1.Checked || this.checkBox2.Checked || this.checkBox5.Checked || this.checkBox6.Checked || this.checkBox7.Checked || this.checkBox8.Checked || this.checkBox10.Checked || this.checkBox11.Checked) { this.HWIDPacking(Environment.CurrentDirectory + "\\Packed.exe"); File.Delete(Environment.CurrentDirectory + "\\Obfuscated.exe"); }
                            else { this.HWIDPacking(Environment.CurrentDirectory + "\\Packed.exe"); File.Delete(Environment.CurrentDirectory + "\\Obfuscated.exe"); }
                            isObf = true;
                        }
                    }
                    if (radioButton3.Checked)
                    {
                        if (this.checkBox1.Checked || this.checkBox2.Checked || this.checkBox5.Checked || this.checkBox6.Checked || this.checkBox7.Checked || this.checkBox8.Checked || this.checkBox10.Checked || this.checkBox11.Checked) { this.USBPacking(Environment.CurrentDirectory + "\\Packed.exe"); File.Delete(Environment.CurrentDirectory + "\\Obfuscated.exe"); }
                        else { this.USBPacking(Environment.CurrentDirectory + "\\Packed.exe"); File.Delete(Environment.CurrentDirectory + "\\Obfuscated.exe"); }
                        isObf = true;
                    }
                    if (radioButton2.Checked)
                    {
                        if (string.IsNullOrEmpty(this.textBox4.Text)) { MessageBox.Show("No Filename specified! (License File Licensing)", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
                        else
                        {
                            if (this.checkBox1.Checked || this.checkBox2.Checked || this.checkBox5.Checked || this.checkBox6.Checked || this.checkBox7.Checked || this.checkBox8.Checked || this.checkBox10.Checked || this.checkBox11.Checked) { this.LicensePacking(Environment.CurrentDirectory + "\\Packed.exe"); File.Delete(Environment.CurrentDirectory + "\\Obfuscated.exe"); }
                            else { this.LicensePacking(Environment.CurrentDirectory + "\\Packed.exe"); File.Delete(Environment.CurrentDirectory + "\\Obfuscated.exe"); }
                            isObf = true;
                        }
                    }
                }
                if (isObf) { MessageBox.Show("Application successfully protected!", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information); }
			}
            catch (Exception ex) { throw;  MessageBox.Show("Error while obfuscating!\n\n" + ex.Message, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error); }
		}

        private void GetDrives()
        {
            comboBox1.Items.Clear();
            DriveInfo[] drives = DriveInfo.GetDrives();
            foreach (DriveInfo driveInfo in drives)
            {
                if (driveInfo.DriveType == DriveType.Removable)
                {
                    comboBox1.Items.Add(driveInfo.RootDirectory);
                }
            }
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e) { if (textBox2.Text == "") { button2_Click(null, null); } groupBox1.Enabled = radioButton1.Checked; }
        private void radioButton2_CheckedChanged(object sender, EventArgs e) { groupBox2.Enabled = radioButton2.Checked; }
        private void checkBox3_CheckedChanged(object sender, EventArgs e) { groupBox4.Enabled = checkBox3.Checked; }

        private void button2_Click(object sender, EventArgs e) { textBox2.Text = GetHardwareID(); }

        private void textBox2_TextChanged(object sender, EventArgs e) { if (((TextBox)sender).Text == "") { ((TextBox)sender).BackColor = Color.IndianRed; } else { ((TextBox)sender).BackColor = ColorTranslator.FromHtml("#0A0A0A"); } }

        private void button4_Click(object sender, EventArgs e) { MessageBox.Show("This licensing option will let the application run only if its on a computer with the same hardware, otherwise the application won't run", "Explanation - HWID Licensing", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        private void button7_Click(object sender, EventArgs e) { MessageBox.Show("This licensing option will let the application run only if the given license file is valid, otherwise the application won't run.", "Explanation - License File Licensing", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        private void button6_Click(object sender, EventArgs e) { MessageBox.Show("This licensing option will let the application run only if the correct removable drive is present, otherwise the application won't run", "Explanation - Removable Drive Licensing", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        
        private void checkBox1_MouseHover(object sender, EventArgs e)
        {
            string text = null;
            if (sender is CheckBox)
            {
                switch (Convert.ToInt32(((CheckBox)sender).Name.Replace("checkBox", null)))
                {
                    case 1: text = "Encodes All Strings into Base64."; break;
                    case 2: text = "Makes de4dot fail to deobfuscate. (Using Attributes)"; break;
                    case 3: text = "Enable/Disable License Packing"; break;
                    case 5: text = "Confuses de4dot by having types with specific names."; break;
                    case 6: text = "Creates a bunch of useless junk, which makes it hard to decompile manually."; break;
                    case 7: text = "Scrambles instructions to confuses decompilation - From Mindlated"; break;
                    case 8: text = "Renames types & methods with random names."; break;
                    case 10: text = "Creates Junk Math, which makes it hard to decompile manually - From Mindlated"; break;
                    case 11: text = "Prevents IL Disassembly in old .NET Decompilers"; break;
                    case 13: text = "Converts Calls to Callis"; break;
                }
            }
            else if (sender is Button)
            {
                switch (Convert.ToInt32(((Button)sender).Name.Replace("button", null)))
                {
                    case 1: text = "Refresh Drive List"; break;
                    case 2: text = "Calculate Computer HWID"; break;
                    case 3: text = "Protect input file"; break;
                    case 4: text = "Help - Computer HWID Licensing"; break;
                    case 7: text = "Help - License File Licensing"; break;
                    case 6: text = "Help - Removable Drive Licensing"; break;
                }
            }
            else if (sender is ComboBox) { text = "Select Removable Drive"; }
            statusBar1.Text = text;
        }

        private void radioButton3_CheckedChanged(object sender, EventArgs e) { GetDrives(); comboBox1.SelectedIndex = 0; groupBox3.Enabled = radioButton3.Checked; }

        private void button1_Click(object sender, EventArgs e) { GetDrives(); }
        private void Main_Load(object sender, EventArgs e) { updateRGB(); rgb = new RainbowObj(button3.FlatAppearance, "BorderColor", new Transitions.TransitionType_Linear(250)); rgb.Start(); GetDrives(); radioButton1.Checked = true; }

        private void menuItem2_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog() { Title = "Select File", Filter = ".NET Executable File|*.exe|Any files|*.*" };
            if (openFileDialog.ShowDialog() == DialogResult.OK) { InitApp(openFileDialog.FileName); }
        }

        private void InitApp(string path)
        {
            try { asm = ModuleDefMD.Load(path, ModuleDefMD.CreateModuleContext()); AssemblyDef asmDef = asm.Assembly; statusBar1.Text = "Successfully loaded '" + Path.GetFileName(path) + "'!"; menuItem10.Text = Path.GetFileName(path); asmPath = path; button3.Enabled = true; }
            catch (Exception ex) { statusBar1.Text = "Failed to load '" + Path.GetFileName(path) + "'!"; MessageBox.Show("Error while loading file!\n\n" + ex.InnerException + "\n\nSelect another file?", "Error while loading file!", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Error); }
        }

        #region Drag & Drop
        private void Main_DragEnter(object sender, DragEventArgs e) { if (panel1.Visible) { return; } panel1.BringToFront(); panel1.Show(); }
        private void label1_Click(object sender, EventArgs e) { menuItem2_Click(null, null); }
        private void label1_DragLeave(object sender, EventArgs e) { panel1.SendToBack(); panel1.Hide(); }
        private void timer1_Tick(object sender, EventArgs e) { label1.Text += "\n\n(press Esc to close this dialog)"; timer1.Stop(); }
        private void panel1_VisibleChanged(object sender, EventArgs e) { label1.Text = "Drop File Here!"; timer1.Start(); }
        private void Main_KeyDown(object sender, KeyEventArgs e) { if (e.KeyCode == Keys.Escape && panel1.Visible) { panel1.SendToBack(); panel1.Hide(); } }
        #endregion
        private void label1_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                InitApp(files[0]);
            }
            label1_DragLeave(null, null);
        }

        private void label1_DragEnter(object sender, DragEventArgs e) { e.Effect = DragDropEffects.Copy; }

        private void menuItem10_Click(object sender, EventArgs e) { if (menuItem10.Text != "Missing Input File") { Process.Start(Path.GetFullPath(Path.GetDirectoryName(asmPath))); } else { menuItem2_Click(null, null); } }

        private void button3_EnabledChanged(object sender, EventArgs e) { if (button3.Enabled) { button3.Text = "Protect"; } else { button3.Text = ""; } }
        private void updateRGB()
        {
            if (Properties.Settings.Default.rgb_MainMenu)
            {
                rgb2 = new RainbowObj(groupBox4, "ForeColor", new Transitions.TransitionType_Linear(100)); rgb3 = new RainbowObj(groupBox5, "ForeColor", new Transitions.TransitionType_Linear(100));
                rgb2.Start(); rgb3.Start();
            }
            else
            {
                if (rgb2 != null) { rgb2.Stop(); rgb2 = null; }
                if (rgb3 != null) { rgb3.Stop(); rgb3 = null; }
                groupBox4.ForeColor = groupBox5.ForeColor = ColorTranslator.FromHtml("#eeeeee");
            }
        }

        private void menuItem12_Click(object sender, EventArgs e)
        {
            About about = new About();
            about.Show();
            if (Properties.Settings.Default.rgb_MainMenu) {  rgb2.Stop(); rgb3.Stop(); }
            Enabled = false;
            about.FormClosed += new FormClosedEventHandler(about_FormClosed);
        }

        void about_FormClosed(object sender, FormClosedEventArgs e) { updateRGB(); Enabled = true; }
    }
}
