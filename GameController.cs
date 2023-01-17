namespace ScoreCounter;

public class GameController {
	public readonly Dictionary<Player, PlayerGame> PlayerSessions;
	public readonly IList<Player> Players;

	private Player _currentPlayer;
	private int _currentPlayerIndex;
	public int CurrentFrame { get; private set; }
	private int _currentStep;

	public GameController(IList<Player> players) {
		this.PlayerSessions = players.ToDictionary(p => p, p => new PlayerGame());
		this.Players = players;

		_currentPlayer = players.First();
		_currentPlayerIndex = 0;
		CurrentFrame = 1;
		_currentStep = 0;
	}

	public Player GetCurrentPlayer() {
		return _currentPlayer;
	}

	public TurnResult SubmitTurn(int numPins) {
		GameFrame currentFrameRef = PlayerSessions[_currentPlayer].GameFrames[CurrentFrame];
		int sumPins = currentFrameRef.NumberOfPinsKnockedDown + numPins;

		TurnResult result;

		// Frames are 1-based, steps are 0-based
		
		if(CurrentFrame < 10) {
			result = (sumPins, _currentStep) switch {
				(10, 0) => TurnResult.Strike,
				(10, >= 1) => TurnResult.Spare,
				(_, 0) => TurnResult.AnotherShot,
				(_, 1) => TurnResult.OpenFrame
			};
		} else {
			Console.WriteLine("D: " + (currentFrameRef.Shots.Count > 0 ? currentFrameRef.Shots[0] : "not there") + ", " + sumPins);

			result = _currentStep switch {
				0 => TurnResult.AnotherShot,
				1 when currentFrameRef.Shots[0] == 10 || sumPins == 10 => TurnResult.AnotherShot,
				_ => TurnResult.PlayerFinished
			};
		}

		PlayerSessions[_currentPlayer].GameFrames[CurrentFrame] = currentFrameRef = result switch {
			_ when CurrentFrame == 10 => new GameFrame.LastFrame(currentFrameRef.Shots.Concat(new int[] {numPins}).ToList().AsReadOnly()),
			TurnResult.Strike => new GameFrame.Strike(),
			TurnResult.Spare => new GameFrame.Spare(currentFrameRef.Shots.Concat(new int[] {numPins}).ToList().AsReadOnly()),
			TurnResult.AnotherShot or TurnResult.OpenFrame => new GameFrame.Open(currentFrameRef.Shots.Concat(new int[] {numPins}).ToList().AsReadOnly())
		};

		if(CurrentFrame < 10) {
			if(result == TurnResult.Strike || result == TurnResult.Spare || result == TurnResult.OpenFrame) {
				turnOver();
			} else {
				_currentStep++;
			}
		} else {
			if(result == TurnResult.PlayerFinished) {
				turnOver();

				if(Players.Count == 0) {
					// No more players left; game over
					result = TurnResult.GameOver;
				}
			} else {
				_currentStep++;
			}
		}

		return result;
	}

	private void turnOver() {
		recalculateCumulativeScoresForCurrentPlayer();

		if(CurrentFrame < 10) {
			if(_currentPlayer == Players.Last()) {
				_currentPlayer = Players.First();
				_currentPlayerIndex = 0;
				CurrentFrame++;
			} else {
				_currentPlayer = Players[++_currentPlayerIndex];
			}

			_currentStep = 0;
		} else {
			Players.Remove(_currentPlayer);

			if(Players.Count > 0) {
				// If there are still players left in the game, set the active player to the next (or previous) one
				if(_currentPlayerIndex >= Players.Count) {
					_currentPlayerIndex--;
				}

				_currentPlayer = Players[_currentPlayerIndex];
				_currentStep = 0;
			}

			// Otherwise, the game over condition will be triggered for us
		}
	}

	private void recalculateCumulativeScoresForCurrentPlayer() {
		int runningScore = 0;

		for(int _frame = 1; _frame <= 10; _frame++) {
			int scoreThisFrame = 0;

			GameFrame frame = PlayerSessions[_currentPlayer].GameFrames[_frame];

			if (frame is GameFrame.Pending) {
				break;
			} else if(frame is GameFrame.Open) {
				scoreThisFrame = frame.NumberOfPinsKnockedDown;
			} else {
				scoreThisFrame = 10;
				
				if(_frame < 10) {
					GameFrame nextFrame = PlayerSessions[_currentPlayer].GameFrames[_frame + 1];

					if(nextFrame != null && !(nextFrame is GameFrame.Pending)) {
						if(frame is GameFrame.Strike) {
							scoreThisFrame += nextFrame.NumberOfPinsKnockedDown;
						} else if(frame is GameFrame.Spare) {
							scoreThisFrame += nextFrame.Shots.FirstOrDefault();
						}
					}
				} else {
					// TODO: Implement
				}
			}

			runningScore += scoreThisFrame;

			PlayerSessions[_currentPlayer].CumulativeScoresByFrame[_frame] = runningScore;
		}
	}
}
