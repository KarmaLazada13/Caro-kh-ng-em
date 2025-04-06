using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.IO;
using System.Windows.Threading;

namespace CaroGame
{
	public partial class MainWindow : Window
	{
		private DispatcherTimer thinkingTimer;
		private int thinkingTimeRemaining = 15;
		private bool isTimerRunning = false;
		private DispatcherTimer timer;
		private int secondsElapsed;
		private MediaPlayer mediaPlayer = new MediaPlayer();
		private List<string> musicPaths = new List<string>();
		private int currentMusicIndex = 0;
		private bool isMusicPlaying = true;
		private string musicFilePath = "music_list.txt"; // File lưu danh sách nhạc
		private string playerX;
		private string playerO;
		private int boardSize = 15;
		private int winCondition = 5;
		private Button[,] buttons;
		private string currentTurn = "X";
		private int playerXWins = 0;
		private int playerOWins = 0;
		private int winThreshold = 1;
		private string gameMode;
		public List<string> ColumnHeaders { get; set; }
		public List<string> RowHeaders { get; set; }

		public MainWindow(string playerX = "Player X", string playerO = "Player O", string mode = "Normal")
		{
			InitializeComponent();
			thinkingTimer = new DispatcherTimer();
			thinkingTimer.Interval = TimeSpan.FromSeconds(1);
			thinkingTimer.Tick += ThinkingTimer_Tick;
			// Vô hiệu hóa việc thay đổi kích thước cửa sổ
			this.ResizeMode = ResizeMode.NoResize;
			// Đảm bảo cửa sổ không được maximize
			this.WindowState = WindowState.Normal;
			this.playerX = playerX;
			this.playerO = playerO;
			gameMode = mode;

			if (mode == "BO3") StartTournamentMode(2);
			else if (mode == "BO5") StartTournamentMode(3);

			InitializeBoard();
			DataContext = this;
			InitializeTimer(); // ✅ Chỉ khởi tạo, không Start!

			int size = 15; // Thay đổi theo kích thước bàn cờ
			ColumnHeaders = Enumerable.Range(0, size).Select(i => ((char)('A' + i)).ToString()).ToList();
			RowHeaders = Enumerable.Range(1, size + 1).Select(i => i.ToString()).ToList();
			UpdatePlayerNames();
			LoadMusicList();

			// ▶️ Nếu có nhạc, phát bài đầu tiên
			if (musicPaths.Count > 0)
			{
				PlayMusic(0);
			}
		}
		private void ResetThinkingTimer()
		{
			if (thinkingTimer != null)
			{
				thinkingTimer.Stop(); // ⏹ Dừng timer
			}

			thinkingTimeRemaining = 15; // ⏱ Reset về 15s
			ThinkingTimerText.Text = $"{currentTurn}: {thinkingTimeRemaining}s";
		}
		private void PauseGame_Click(object sender, RoutedEventArgs e)
		{
			

			// Dừng đồng hồ
			timer.Stop();
			thinkingTimer.Stop();
			isTimerRunning = false;

			// Hiện lớp che bàn cờ
			PauseOverlay.Visibility = Visibility.Visible;

			// Hiện thông báo, khi đóng sẽ tiếp tục game
			MessageBox.Show("⏸ Trận đấu đang tạm dừng\nNhấn OK để tiếp tục.", "Tạm dừng");

			// Ẩn lớp che và tiếp tục đồng hồ
			PauseOverlay.Visibility = Visibility.Collapsed;
			timer.Start();
			thinkingTimer.Start();
			isTimerRunning = true;
		}

		private void ThinkingTimer_Tick(object sender, EventArgs e)
		{
			thinkingTimeRemaining--;
			ThinkingTimerText.Text = $"{currentTurn}: {thinkingTimeRemaining}s";

			if (thinkingTimeRemaining <= 0)
			{
				// ⛔ Dừng tất cả đồng hồ NGAY LẬP TỨC
				thinkingTimer.Stop();
				timer.Stop();
				isTimerRunning = false;

				string loser = currentTurn;
				string winner = (currentTurn == "X") ? "O" : "X";

				// 👇 Dùng Dispatcher để delay MessageBox (hiện sau khi UI cập nhật hoàn tất)
				Dispatcher.InvokeAsync(() =>
				{
					MessageBox.Show($"{loser} suy nghĩ quá lâu! {winner} thắng!", "Thời gian hết");
					OnPlayerWin(winner == "X");
				}, DispatcherPriority.Background);
			}
		}



		private bool IsInBounds(int x, int y)
		{
			return x >= 0 && x < boardSize && y >= 0 && y < boardSize;
		}

		private bool GetWinningLine(int x, int y, int dx, int dy, string player, int requiredWin, out List<(int, int)> result)
		{
			result = new List<(int, int)>();
			List<(int, int)> temp = new List<(int, int)>();
			int count = 1;
			temp.Add((x, y));

			// Forward
			int i = x + dx;
			int j = y + dy;
			while (IsInBounds(i, j) && buttons[i, j].Content?.ToString() == player)
			{
				temp.Add((i, j));
				count++;
				i += dx;
				j += dy;
			}

			// Backward
			i = x - dx;
			j = y - dy;
			while (IsInBounds(i, j) && buttons[i, j].Content?.ToString() == player)
			{
				temp.Add((i, j));
				count++;
				i -= dx;
				j -= dy;
			}

			if (count >= requiredWin)
			{
				result = temp;
				return true;
			}

			return false;
		}

		private void HighlightWinningMoves(List<(int, int)> winningMoves)
		{
			foreach (var (i, j) in winningMoves)
			{
				buttons[i, j].Background = Brushes.Yellow;
			}
		}
		private void ResetTimer()
		{
			timer.Stop();  // ⏹ Dừng đồng hồ
			secondsElapsed = 0;  // 🔄 Reset thời gian
			TimerText.Text = "Time: 0s";  // 🕒 Cập nhật giao diện
			isTimerRunning = false;  // 🚫 Không tự động chạy, chờ nước đi đầu tiên
		}

		private void Timer_Tick(object sender, EventArgs e)
		{
			secondsElapsed++;
			TimerText.Text = $"Time: {secondsElapsed}s";
		}
		private void InitializeTimer()
		{
			timer = new DispatcherTimer();
			timer.Interval = TimeSpan.FromSeconds(1);
			timer.Tick += Timer_Tick;
			secondsElapsed = 0;
			TimerText.Text = "Time: 0s";
		}
		private void DeleteSelectedMusic_Click(object sender, RoutedEventArgs e)
		{
			if (MusicListComboBox.SelectedItem == null)
			{
				MessageBox.Show("Vui lòng chọn một bài nhạc để xóa.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			string selectedMusic = MusicListComboBox.SelectedItem.ToString();

			// Đảm bảo không xóa nhạc mặc định
			if (selectedMusic == "music1.mp3" || selectedMusic == "music2.mp3" || selectedMusic == "music3.mp3")
			{
				MessageBox.Show("Không thể xóa nhạc mặc định!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			// Xóa khỏi danh sách và cập nhật file
			musicPaths.Remove(selectedMusic);
			File.WriteAllLines(musicFilePath, musicPaths);

			// Cập nhật lại danh sách trong giao diện
			UpdateMusicComboBox();

			MessageBox.Show($"Đã xóa: {selectedMusic}", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
		}
		private void LoadMusicList()
		{
			// Thêm nhạc mặc định
			if (!File.Exists(musicFilePath))
			{
				File.WriteAllLines(musicFilePath, new[] { "bank" }); // Tùy chỉnh nhạc mặc định

			}

			musicPaths = File.ReadAllLines(musicFilePath).ToList();
			UpdateMusicComboBox();
		}
		private void UpdateMusicComboBox()
		{
			MusicListComboBox.ItemsSource = null;  // Xóa dữ liệu cũ
			MusicListComboBox.ItemsSource = musicPaths; // Cập nhật danh sách mới

			if (musicPaths.Count > 0)
			{
				MusicListComboBox.SelectedIndex = 0; // Chọn bài hát đầu tiên sau khi cập nhật
			}
		}


		private void PlayMusic(int index)
		{
			if (musicPaths.Count == 0 || index < 0 || index >= musicPaths.Count) return;

			currentMusicIndex = index;
			string musicPath = musicPaths[index];

			if (File.Exists(musicPath))
			{
				mediaPlayer.Open(new Uri(musicPath));
				mediaPlayer.MediaEnded += (s, e) => NextMusic();
				mediaPlayer.Play();
			}
		}

		private void SelectMusic_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog
			{
				Filter = "Audio Files|*.mp3;*.wav"
			};

			if (openFileDialog.ShowDialog() == true)
			{
				string selectedMusicPath = openFileDialog.FileName;

				if (!musicPaths.Contains(selectedMusicPath))
				{
					musicPaths.Add(selectedMusicPath);
					File.AppendAllLines(musicFilePath, new[] { selectedMusicPath });
					UpdateMusicComboBox();
				}

				PlayMusic(musicPaths.Count - 1);
			}
		}

		private void MusicListComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (MusicListComboBox.SelectedIndex != -1)
			{
				PlayMusic(MusicListComboBox.SelectedIndex);
			}
		}



		private void NextMusic_Click(object sender, RoutedEventArgs e)
		{
			NextMusic();
		}

		private void NextMusic()
		{
			if (musicPaths.Count > 0)
			{
				currentMusicIndex = (currentMusicIndex + 1) % musicPaths.Count;
				PlayMusic(currentMusicIndex);
				MusicListComboBox.SelectedIndex = currentMusicIndex;
			}
		}
		private void ToggleMusic_Click(object sender, RoutedEventArgs e)
		{
			if (isMusicPlaying)
			{
				mediaPlayer.Pause();
				ToggleMusicMenuItem.Header = "Bật nhạc";
			}
			else
			{
				mediaPlayer.Play();
				ToggleMusicMenuItem.Header = "Tắt nhạc";
			}
			isMusicPlaying = !isMusicPlaying;
		}

		private void UpdatePlayerNames()
		{
			if (ScoreXTextBox != null && ScoreOTextBox != null)
			{
				ScoreXTextBox.Text = $"{playerX}: {playerXWins}";
				ScoreOTextBox.Text = $"{playerO}: {playerOWins}";
			}
		}
		private void StartTournamentMode(int threshold)
		{
			winThreshold = threshold;
			playerXWins = 0;
			playerOWins = 0;
			ScoreXTextBox.Visibility = Visibility.Visible;
			ScoreOTextBox.Visibility = Visibility.Visible;
			UpdateScoreDisplay();
		}

		private void UpdateScoreDisplay()
		{
			ScoreXTextBox.Text = $"Player X: {playerXWins}";
			ScoreOTextBox.Text = $"Player O: {playerOWins}";
		}

		private void OnPlayerWin(bool isXWin)
		{
			thinkingTimer.Stop();
			timer.Stop(); // ⏹ Dừng đồng hồ khi có người thắng
			isTimerRunning = false; // 🚫 Đảm bảo timer không tự chạy ngay ván mới
			secondsElapsed = 0; // 🔄 Reset thời gian về 0
			TimerText.Text = "Time: 0s"; // ⏳ Cập nhật giao diện

			if (isXWin) playerXWins++;
			else playerOWins++;

			UpdateScoreDisplay();

			// 🔥 Kiểm tra nếu đang chơi BO3 hoặc BO5
			if ((gameMode == "BO3" || gameMode == "BO5") && (playerXWins == winThreshold || playerOWins == winThreshold))
			{
				MessageBox.Show($"🏆 {(playerXWins == winThreshold ? playerX : playerO)} thắng giải đấu!", "Kết thúc giải đấu");
				ResetTournament();
			}
			else
			{
				MessageBox.Show($"🎉 Người chơi {(isXWin ? playerX : playerO)} thắng!", "Kết thúc ván đấu");
				ResetThinkingTimer();
				ResetBoard(); // Chỉ reset bàn cờ, không tạo lại toàn bộ UI
				UpdatePlayerNames(); // Cập nhật lại tên người chơi trên giao diện
				ResetTimer(); // 🔄 Reset đồng hồ cho ván mới
				
			}
		}



		private void ResetTournament()
		{
			playerXWins = 0;
			playerOWins = 0;
			ScoreXTextBox.Visibility = Visibility.Hidden;
			ScoreOTextBox.Visibility = Visibility.Hidden;
			ResetThinkingTimer();
			ResetBoard();
			ResetTimer();
		}

		private void ResetBoard()
		{
			foreach (var btn in BoardGrid.Children)
			{
				if (btn is Button button)
				{
					button.Content = ""; // Xóa nội dung của nút
					button.Background = Brushes.LightGray; // Reset lại màu nền của nút
				}
			}
			currentTurn = "X"; // Đặt lại người chơi bắt đầu
		}


		private void InitializeBoard()
		{
			BoardGrid.Children.Clear();
			buttons = new Button[boardSize, boardSize];

			// Cập nhật số hàng và cột của UniformGrid
			BoardGrid.Rows = boardSize;
			BoardGrid.Columns = boardSize;

			for (int i = 0; i < boardSize; i++)
			{
				for (int j = 0; j < boardSize; j++)
				{
					Button btn = new Button
					{
						FontSize = 16,
						HorizontalAlignment = HorizontalAlignment.Stretch,
						VerticalAlignment = VerticalAlignment.Stretch
					};

					btn.Click += Button_Click;
					buttons[i, j] = btn;

					BoardGrid.Children.Add(btn);
				}
			}
		}
		private bool CheckWin(string player, out List<(int, int)> winningMoves)
		{
			winningMoves = new List<(int, int)>();
			int requiredWin = winCondition;

			for (int i = 0; i < boardSize; i++)
			{
				for (int j = 0; j < boardSize; j++)
				{
					if (buttons[i, j].Content?.ToString() == player)
					{
						List<(int, int)> tempWinningMoves = new List<(int, int)>();

						// Kiểm tra các hướng (ngang, dọc, chéo phải xuống, chéo trái xuống)
						if (GetWinningLine(i, j, 1, 0, player, requiredWin, out tempWinningMoves) ||    // Ngang
							GetWinningLine(i, j, 0, 1, player, requiredWin, out tempWinningMoves) ||    // Dọc
							GetWinningLine(i, j, 1, 1, player, requiredWin, out tempWinningMoves) ||    // Chéo phải xuống
							GetWinningLine(i, j, 1, -1, player, requiredWin, out tempWinningMoves))     // Chéo trái xuống
						{
							winningMoves.AddRange(tempWinningMoves); // Nối kết quả vào danh sách chính
							return true;
						}
					}
				}
			}

			return false;
		}


		private int CountConsecutive(int row, int col, int dRow, int dCol, string player)
		{
			int count = 0;
			while (row >= 0 && row < boardSize && col >= 0 && col < boardSize &&
		   buttons[row, col].Content != null && buttons[row, col].Content.ToString() == player)
			{
				count++;
				row += dRow;
				col += dCol;
			}
			return count;
		}
		private void SetSize_Click(object sender, RoutedEventArgs e)
		{
			if (sender is MenuItem menuItem && int.TryParse(menuItem.Tag.ToString(), out int size))
			{
				boardSize = size;

				if (boardSize == 3) winCondition = 3;
				else winCondition = 5;

				winConditionTextBox.Text = $"{winCondition} quân thắng";
				InitializeBoard();
				ResetThinkingTimer();
				ResetTimer();
				
			}
		}

		private void SetWinCondition_Click(object sender, RoutedEventArgs e)
		{
			if (sender is MenuItem menuItem && int.TryParse(menuItem.Tag.ToString(), out int condition))
			{
				winCondition = condition;
				if (winCondition == 3) boardSize = 3;
				ResetThinkingTimer();
				ResetTimer();
				InitializeBoard();
				
			}
		}

		private void RestartGame_Click(object sender, RoutedEventArgs e)
		{
			ResetThinkingTimer();
			ResetTimer(); // ⏳ Reset đồng hồ khi chơi lại
			InitializeBoard();
		}

		private void BackToGameMode_Click(object sender, RoutedEventArgs e)
		{
			mediaPlayer.Stop(); // Tắt nhạc khi quay lại màn hình chính
			isMusicPlaying = false; // Đảm bảo nhạc không tự bật lại sau đó

			timer.Stop();            // Dừng tổng thời gian
			thinkingTimer.Stop();    // ⛔ Dừng đồng hồ suy nghĩ!

			GameModeWindow gameModeWindow = new GameModeWindow();
			gameModeWindow.Show();
			this.Close(); // Đóng cửa sổ hiện tại
		}




		// Tìm nước đi giúp thắng ngay hoặc chặn đối thủ
		private (int, int) FindWinningMove(string player)
		{
			// Tạo danh sách tạm thời cho winningMoves
			List<(int, int)> winningMoves = new List<(int, int)>();

			for (int i = 0; i < boardSize; i++)
			{
				for (int j = 0; j < boardSize; j++)
				{
					if (string.IsNullOrEmpty(buttons[i, j].Content?.ToString()))
					{
						buttons[i, j].Content = player;

						// Gọi CheckWin và truyền tham số out winningMoves
						if (CheckWin(player, out winningMoves))
						{
							buttons[i, j].Content = ""; // Hoàn tác thử nghiệm
							return (i, j);
						}

						buttons[i, j].Content = ""; // Hoàn tác thử nghiệm
					}
				}
			}
			return (-1, -1); // Không tìm thấy nước đi chiến thắng
		}


		// Chọn nước đi tốt nhất dựa trên vị trí gần các quân cờ
		private (int, int) FindBestMove()
		{
			for (int i = 0; i < boardSize; i++)
			{
				for (int j = 0; j < boardSize; j++)
				{
					if (string.IsNullOrEmpty(buttons[i, j].Content?.ToString()) && HasAdjacentPiece(i, j))
					{
						return (i, j);
					}
				}
			}
			return (-1, -1);
		}

		// Kiểm tra xem một ô có gần quân cờ nào không
		private bool HasAdjacentPiece(int row, int col)
		{
			int[] dx = { -1, -1, -1, 0, 0, 1, 1, 1 };
			int[] dy = { -1, 0, 1, -1, 1, -1, 0, 1 };

			for (int i = 0; i < 8; i++)
			{
				int newRow = row + dx[i];
				int newCol = col + dy[i];

				if (newRow >= 0 && newRow < boardSize && newCol >= 0 && newCol < boardSize)
				{
					if (!string.IsNullOrEmpty(buttons[newRow, newCol].Content?.ToString()))
					{
						return true;
					}
				}
			}
			return false;
		}

		// Nếu không có nước đi tốt, chọn ô ngẫu nhiên
		private void RandomMove()
		{
			Random rand = new Random();
			while (true)
			{
				int row = rand.Next(boardSize);
				int col = rand.Next(boardSize);
				if (string.IsNullOrEmpty(buttons[row, col].Content?.ToString()))
				{
					buttons[row, col].Content = "O";
					buttons[row, col].Foreground = Brushes.Red;
					return;
				}
			}
		}


		private void Button_Click(object sender, RoutedEventArgs e)
		{
			Button btn = sender as Button;
			if (btn != null && string.IsNullOrEmpty(btn.Content?.ToString()))
			{
				btn.Content = currentTurn;
				btn.Foreground = currentTurn == "X" ? Brushes.DeepSkyBlue : Brushes.Red;

				// 🕐 Bắt đầu đồng hồ tổng nếu chưa chạy
				if (!isTimerRunning)
				{
					timer.Start();
					isTimerRunning = true;
				}

				// ✅ Kiểm tra thắng
				List<(int, int)> winningMoves = new List<(int, int)>();
				if (CheckWin(currentTurn, out winningMoves))
				{
					HighlightWinningMoves(winningMoves);
					timer.Stop();
					thinkingTimer.Stop();
					OnPlayerWin(currentTurn == "X");
					return;
				}

				// 🔁 Đổi lượt
				currentTurn = (currentTurn == "X") ? "O" : "X";

				// ⏱ Chỉ reset đồng hồ suy nghĩ nếu KHÔNG phải chế độ AI
				if (gameMode != "AI")
				{
					thinkingTimer.Stop();
					thinkingTimeRemaining = 15;
					ThinkingTimerText.Text = $"{currentTurn}: {thinkingTimeRemaining}s";
					thinkingTimer.Start();
				}

				// 🤖 Nếu là chế độ AI và tới lượt AI thì cho AI đánh
				if (gameMode == "AI" && currentTurn == "O")
				{
					MakeAIMove();
				}
			}
		}





		// Fix lỗi AI Move
		private void MakeAIMove()
		{
			if (currentTurn != "O") return;

			var bestMove = FindWinningMove("O");
			if (bestMove != (-1, -1))
			{
				buttons[bestMove.Item1, bestMove.Item2].Content = "O";
				buttons[bestMove.Item1, bestMove.Item2].Foreground = Brushes.Red;
			}
			else
			{
				bestMove = FindBlockingMove();
				if (bestMove == (-1, -1))
				{
					bestMove = FindBestMove();
				}

				if (bestMove != (-1, -1))
				{
					buttons[bestMove.Item1, bestMove.Item2].Content = "O";
					buttons[bestMove.Item1, bestMove.Item2].Foreground = Brushes.Red;
				}
				else
				{
					RandomMove();
				}
			}

			// Kiểm tra chiến thắng sau khi AI di chuyển và highlight nước đi chiến thắng
			List<(int, int)> winningMoves = new List<(int, int)>();
			if (CheckWin("O", out winningMoves))
			{
				HighlightWinningMoves(winningMoves);

				// ⏹ Dừng cả hai loại timer NGAY LẬP TỨC trước khi hiện thông báo
				thinkingTimer.Stop();
				timer.Stop();
				isTimerRunning = false;
				secondsElapsed = 0;

				// ⏳ Reset giao diện hiển thị thời gian
				TimerText.Text = "Time: 0s";
				ThinkingTimerText.Text = "O: 15s";

				MessageBox.Show("AI thắng!");
				ResetBoard();
				ResetTimer(); // để chuẩn bị ván mới nếu chơi tiếp
				return;
			}

			currentTurn = "X";
		}



		private (int, int) FindBlockingMove()
		{
			// Kiểm tra xem đối thủ có thể thắng ở lượt tiếp theo không
			var fourMove = FindWinningMove("X");
			if (fourMove != (-1, -1)) return fourMove;

			// Kiểm tra nước đi nào của đối thủ tạo ra 3 quân liên tiếp có khoảng trống hai đầu
			for (int i = 0; i < boardSize; i++)
			{
				for (int j = 0; j < boardSize; j++)
				{
					if (string.IsNullOrEmpty(buttons[i, j].Content?.ToString()))
					{
						buttons[i, j].Content = "X";
						bool canFormThree = CanFormOpenThree(i, j, "X"); // Kiểm tra nếu đối thủ có thể mở rộng lên 4 quân
						buttons[i, j].Content = ""; // Hoàn tác thử nghiệm

						if (canFormThree) return (i, j); // Nếu có, chọn ô này để chặn
					}
				}
			}
			return (-1, -1);
		}

		// Kiểm tra xem có thể tạo thành 3 quân liên tiếp có khoảng trống hai đầu hay không
		private bool CanFormOpenThree(int row, int col, string player)
		{
			return (CountConsecutive(row, col, 1, 0, player) == 3 && IsOpen(row, col, 1, 0)) ||  // Kiểm tra ngang
				   (CountConsecutive(row, col, 0, 1, player) == 3 && IsOpen(row, col, 0, 1)) ||  // Kiểm tra dọc
				   (CountConsecutive(row, col, 1, 1, player) == 3 && IsOpen(row, col, 1, 1)) ||  // Kiểm tra chéo phải xuống
				   (CountConsecutive(row, col, 1, -1, player) == 3 && IsOpen(row, col, 1, -1));  // Kiểm tra chéo trái xuống
		}

		// Kiểm tra xem dãy 3 quân liên tiếp có bị chặn hay không
		private bool IsOpen(int row, int col, int dRow, int dCol)
		{
			int newRow1 = row - dRow, newCol1 = col - dCol;
			int newRow2 = row + 3 * dRow, newCol2 = col + 3 * dCol;

			bool openStart = (newRow1 >= 0 && newRow1 < boardSize && newCol1 >= 0 && newCol1 < boardSize &&
							  string.IsNullOrEmpty(buttons[newRow1, newCol1].Content?.ToString()));
			bool openEnd = (newRow2 >= 0 && newRow2 < boardSize && newCol2 >= 0 && newCol2 < boardSize &&
							string.IsNullOrEmpty(buttons[newRow2, newCol2].Content?.ToString()));

			return openStart && openEnd; // Chỉ coi là nguy hiểm nếu cả hai đầu đều trống
		}

	}
}
