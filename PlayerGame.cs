namespace ScoreCounter;

public class PlayerGame {
    public readonly Dictionary<int, GameFrame> GameFrames;
	public readonly Dictionary<int, int> CumulativeScoresByFrame;

	public PlayerGame() {
		// Pre-populate the 10 frames for the game
		this.GameFrames = Enumerable.Range(1, 10).ToDictionary(k => k, _ => (GameFrame)(new GameFrame.Pending()));
		this.CumulativeScoresByFrame = Enumerable.Range(1, 10).ToDictionary(k => k, _ => 0);
	}

	public int GetCumulativeScoreForFrame(int frame) {
		return CumulativeScoresByFrame[frame];
	}
}
