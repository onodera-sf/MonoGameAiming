using System;

namespace Aiming
{
	public static class Program
	{
		[STAThread]
		static void Main()
		{
			using (var game = new AimingGame())
				game.Run();
		}
	}
}
