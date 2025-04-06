using System.Windows;

namespace CaroGame
{
	public partial class GameModeWindow : Window
	{
		public GameModeWindow()
		{
			InitializeComponent();
		}

		private void OpenMainWindow(string mode)
		{
			string playerX = PlayerXName.Text; // Lấy tên Player X
			string playerO = PlayerOName.Text; // Lấy tên Player O

			MainWindow mainWindow = new MainWindow(playerX, playerO, mode);
			mainWindow.Show();
			this.Close();
		}

		private void NormalMode_Click(object sender, RoutedEventArgs e)
		{
			OpenMainWindow("Normal");
		}

		private void BO3_Click(object sender, RoutedEventArgs e)
		{
			OpenMainWindow("BO3");
		}

		private void BO5_Click(object sender, RoutedEventArgs e)
		{
			OpenMainWindow("BO5");
		}

		private void PlayAI_Click(object sender, RoutedEventArgs e)
		{
			OpenMainWindow("AI");
		}
	}
}

