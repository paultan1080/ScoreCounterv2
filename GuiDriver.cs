namespace ScoreCounter;

public class GuiDriver {
	// Number of recent output lines to keep
	const int LOG_DISPLAY_SIZE = 10;
	private readonly int _scoreboardWidth;

	private readonly GameController _game;

	private readonly Random _random = new Random();	
	private readonly int _longestPlayerNameLength;		// For padding
	
	// Pre-rendered horizontal separator that spans the correct width
	private readonly string _horizontalSeparator;
	// Pre-rendered winner banner
	private readonly string _winnerBanner;
	// Pre-rendered title banner
	private readonly string _titleBanner;

	// Saves the default console color so we can revert
	private readonly ConsoleColor _defaultColor;

	// Contains the recent output so it can be redrawn
	private readonly LinkedList<(ConsoleColor, string)> _outputLogs;

	// Used by drawState() to highlight the winner at the end of the game
	private Player? _gameWinner = null;

	public GuiDriver(GameController game) {
		this._game = game;

		this._longestPlayerNameLength = game.PlayerSessions.Keys.Max(x => x.Name.Length);

		// Normal cells are a total of 8 characters wide (3 for each step, 1 for central separator, and 1 for right border)
		// The last cell is 12 characters (11 without right border), since it has three steps.
		this._scoreboardWidth = 2 + _longestPlayerNameLength + 2 + (8 * 9) + 12;

		this._horizontalSeparator = new String('-', _scoreboardWidth);
		
		const string titleText = "Bowling ScoreCounter";
		this._titleBanner = new String(' ', (_horizontalSeparator.Length - titleText.Length) / 2) + titleText;

		const string winnerText = "!!!WINNER!!!";
		this._winnerBanner = new String(' ', (_horizontalSeparator.Length - winnerText.Length) / 2) + winnerText + new String(' ', (_horizontalSeparator.Length - winnerText.Length) / 2);

		this._defaultColor = Console.ForegroundColor;

		this._outputLogs = new LinkedList<(ConsoleColor, string)>();
	}

	public void Play() {
		void _writeAndLogWithColor(string text, ConsoleColor color) {
			writeWithColor(text, color);
			_outputLogs.AddLast((color, text));
		};

		bool isGameOver = false;

		void _writeAndLogWithoutColor(string text) {
			writeWithColor(text, _defaultColor);
			_outputLogs.AddLast((_defaultColor, text));
		};

		while(!isGameOver) {
			drawState();
			Player currentPlayer = _game.GetCurrentPlayer();

			int remainingPins;
			if(_game.CurrentFrame != 10) {
				remainingPins = 10 - _game.PlayerSessions[currentPlayer].GameFrames[_game.CurrentFrame].NumberOfPinsKnockedDown;
			} else {
				if(_game.PlayerSessions[currentPlayer].GameFrames[_game.CurrentFrame].Shots.Count <= 1) {
					remainingPins = 10 - _game.PlayerSessions[currentPlayer].GameFrames[_game.CurrentFrame].NumberOfPinsKnockedDown;
				} else if(_game.PlayerSessions[currentPlayer].GameFrames[_game.CurrentFrame].Shots.Count >= 2 && _game.PlayerSessions[currentPlayer].GameFrames[_game.CurrentFrame].Shots[1] != 10) {
					remainingPins = 10 - _game.PlayerSessions[currentPlayer].GameFrames[_game.CurrentFrame].Shots[1];
				} else {
					remainingPins = 10;
				}
			}

			int numPins = promptTurn(currentPlayer, _game.CurrentFrame, remainingPins);

			switch(_game.SubmitTurn(numPins)) {
				case TurnResult.Strike:
					_writeAndLogWithoutColor("WOOHOO!!! Congratulations, you got a STRIKE!");
					break;
				case TurnResult.Spare:
					_writeAndLogWithoutColor("Way to go! That was a SPARE!");
					break;
				case TurnResult.AnotherShot:
					remainingPins = 10 - _game.PlayerSessions[currentPlayer].GameFrames[_game.CurrentFrame].NumberOfPinsKnockedDown;

					if(_game.CurrentFrame != 10) {
						if(numPins > 5) {
							_writeAndLogWithoutColor($"Nice! Only {remainingPins} to go.");
						} else {
							_writeAndLogWithoutColor($"Ouch! Good luck with the {remainingPins} remaining ones.");
						}
					} else {
						_writeAndLogWithoutColor("Nice try!");
					}
					break;
				case TurnResult.OpenFrame:
					_writeAndLogWithoutColor("Frame is LEFT OPEN. Better luck next time!");
					break;
				case TurnResult.PlayerFinished:
					_writeAndLogWithoutColor($"{currentPlayer} is out!");
					break;
				case TurnResult.GameOver:
					var winner = _game.PlayerSessions.OrderByDescending(x => x.Value.CumulativeScoresByFrame[10]).First();
					_writeAndLogWithColor($"GAME OVER!!! Player {winner.Key.Name} is the winner, with a score of {winner.Value.CumulativeScoresByFrame[10]}!", ConsoleColor.White);
					isGameOver = true;
					_gameWinner = winner.Key;

					// Re-draw state one last time to show the final state of the game
					drawState();

					break;
			}
		}


		Console.WriteLine();
		Console.WriteLine("Thank you for playing!");
		Console.WriteLine("See you again soon... Press any key to exit.");
		Console.ReadKey();
	}

	private int promptTurn(Player player, int frame, int maxValue) {
		(int? value, string error) parseResult(string? input) {
			if(String.IsNullOrWhiteSpace(input)) {
				return (_random.Next(1, maxValue + 1), String.Empty);
			}

			if(Int32.TryParse(input, out int result)) {
				if(result > maxValue) {
					return (null, "Invalid input! That would be too many pins.");
				}

				return (result, String.Empty);
			}

			return (null, "Invalid input! Please enter the number of pins knocked last turn.");
		}

		int pins = 0;
		(int? value, string error) result;
		
		writeWithColor($"[{player.Name}] Frame {frame}: Your turn (enter number of pins knocked down, or enter for random): ", player.Color);
		
		while((result = parseResult(Console.ReadLine())).value == null) {
			Console.WriteLine(result.error);
		}

		pins = result.value.Value;

		_outputLogs.AddLast((player.Color, $"[{player.Name}] Frame {frame}: Your turn (enter number of pins knocked down, or enter for random): {pins}"));

		return pins;
	}

	private void drawState() {
		void _drawWinnerBanner() {
			ConsoleColor fg = Console.ForegroundColor;
			ConsoleColor bg = Console.BackgroundColor;
			Console.ForegroundColor = ConsoleColor.White;
			Console.BackgroundColor = ConsoleColor.DarkRed;
			Console.WriteLine(_winnerBanner);
			Console.ForegroundColor = fg;
			Console.BackgroundColor = bg;
		}

		Console.Clear();

		// Render Scoreboard

		Console.WriteLine(_titleBanner);

		// Normal cells are a total of 8 characters wide (3 for each step, 1 for central separator, and 1 for right border)
		// The last cell is 12 characters (11 without right border), since it has three steps.

		Console.WriteLine();
		Console.WriteLine(
			// Left margin
			new String(' ', 4 + _longestPlayerNameLength) + 
			// Headers 1-9
			String.Join('|', Enumerable.Range(1, 9).Select(f => $"   {f}   ")) + 
			// Last header (10), since it is a different width
			"     10    " );
		Console.WriteLine(_horizontalSeparator);

		foreach(KeyValuePair<Player, PlayerGame> playerGame in _game.PlayerSessions) {
			// Is this player the winner?
			if(_gameWinner == playerGame.Key) {
				_drawWinnerBanner();
			}

			Console.Write("| ");

			// Write player name
			writeWithColor(playerGame.Key.Name.PadLeft(_longestPlayerNameLength), playerGame.Key.Color);

			Console.Write(" |");

			// Loop through the frames
			foreach(KeyValuePair<int, GameFrame> frame in playerGame.Value.GameFrames) {
				// Check if we are in the active frame, so we can highlight it blue
				bool isActiveFrame = _game.CurrentFrame == frame.Key && _game.GetCurrentPlayer() == playerGame.Key;

				if(isActiveFrame) {
					Console.BackgroundColor = ConsoleColor.DarkBlue;
				}

				switch(frame.Value) {
					case GameFrame.Pending p when frame.Key < 10:
						Console.Write("   |   ");
						break; 
					case GameFrame.Pending p when frame.Key == 10:
						Console.Write("   |   |   ");
						break; 
					case GameFrame.Strike s:
						Console.Write("   | ");
						writeWithColor("X ", playerGame.Key.Color);
						break; 
					case GameFrame.Spare s:
						Console.Write(' ');
						writeWithColor(s.Shots.First().ToString(), playerGame.Key.Color);
						Console.Write($" | ");
						writeWithColor("/ ", playerGame.Key.Color);
						break; 
					case GameFrame.Open o:
						Console.Write(' ');
						writeWithColor(o.Shots.First().ToString(), playerGame.Key.Color);
						Console.Write(" | ");
						if(o.Shots.Count > 1) {
							writeWithColor(o.Shots.Last().ToString(), playerGame.Key.Color);
							Console.Write(' ');
						} else {
							Console.Write("  ");
						}

						break; 
					case GameFrame.LastFrame l:
						Console.Write(' ');

						int takenShots = l.Shots.Count;

						if(takenShots >= 1) {
							writeWithColor(l.Shots[0] == 10 ? "X" : l.Shots[0].ToString(), playerGame.Key.Color);
						} else {
							Console.Write(" ");
						}
						
						Console.Write(" | ");

						if(takenShots >= 2) {
							char symbol;

							if(l.Shots[1] == 10) {
								symbol = 'X';
							} else if(l.Shots[1] == 0) {
								symbol = '-';
							} else if (l.Shots[0] + l.Shots[1] == 10) {
								symbol = '/';
							} else {
								symbol = l.Shots[1].ToString()[0];
							}

							writeWithColor(symbol.ToString(), playerGame.Key.Color);
						} else {
							Console.Write(" ");
						}
						
						Console.Write(" | ");

						if(takenShots >= 3) {
							char symbol;

							if(l.Shots[2] == 10) {
								symbol = 'X';
							} else if(l.Shots[2] == 0) {
								symbol = '-';
							} else if (
								l.Shots[2] + l.Shots[1] == 10 && 
								(l.Shots[0] + l.Shots[1] != 10)	// Make sure the previous one wasn't a spare (i.e. in case of 1,9,1)
							) {
								symbol = '/';
							} else {
								symbol = l.Shots[2].ToString()[0];
							}

							writeWithColor(symbol.ToString() + " ", playerGame.Key.Color);
						} else {
							Console.Write("  ");
						}

						break; 
				}

				if(isActiveFrame) {
					Console.BackgroundColor = ConsoleColor.Black;
				}

				Console.Write('|');
			}

			// Draw the second line of the score frame cells

			Console.WriteLine();
			Console.Write("|");
			Console.Write(new String(' ', _longestPlayerNameLength + 1));
			Console.Write(" |");

			foreach(KeyValuePair<int, GameFrame> frame in playerGame.Value.GameFrames) {
				Console.Write(frame.Key == 10 ? "    " : "  ");

				string playerScore = playerGame.Value.GetCumulativeScoreForFrame(frame.Key).ToString();
				if(playerScore == "0") {
					playerScore = " ";
				}

				writeWithColor(playerScore.PadRight(frame.Key == 10 ? 7 : 5), playerGame.Key.Color);

				Console.Write('|');
			}

			Console.WriteLine();
			
			// Is this player the winner?
			if(_gameWinner == playerGame.Key) {
				_drawWinnerBanner();
			}

			Console.WriteLine(_horizontalSeparator);
		}

		// Render text from output logs
		
		Console.WriteLine();
		Console.WriteLine();

		// Trim log if we are above 10 lines
		while(_outputLogs.Count > LOG_DISPLAY_SIZE) {
			_outputLogs.RemoveFirst();
		}

		foreach((ConsoleColor, string) log in _outputLogs) {
			writeWithColor(log.Item2, log.Item1);
			Console.WriteLine();
		}
	}

	private void writeWithColor(string text, ConsoleColor color) {
		Console.ForegroundColor = color;
		Console.Write(text);
		Console.ForegroundColor = _defaultColor;
	}
}
