using System;
using System.Windows.Forms;

namespace NetShield_Protector
{
	// Token: 0x02000003 RID: 3
	internal static class Program
	{
		// Token: 0x06000023 RID: 35 RVA: 0x000067D5 File Offset: 0x000049D5
		[STAThread]
		private static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new Main());
		}
	}
}
