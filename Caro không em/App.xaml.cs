using CaroGame;
using System.Configuration;
using System.Data;
using System.Windows;

namespace Caro_không_em
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			// Mở cửa sổ Game Mode đầu tiên
			GameModeWindow gameModeWindow = new GameModeWindow();
			gameModeWindow.Show();
		}


	}

}
