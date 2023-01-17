namespace ScoreCounter;

public abstract record GameFrame(IList<int> Shots) {
	public sealed record Pending() : GameFrame(EmptyList);
	public sealed record Strike() : GameFrame(StrikeList);
	public sealed record Spare(IList<int> Shots) : GameFrame(Shots);
	public sealed record Open(IList<int> Shots) : GameFrame(Shots);
	public sealed record LastFrame(IList<int> Shots) : GameFrame(Shots);

	public int NumberOfPinsKnockedDown => Shots.Sum();
	// public void AddShot(int numPins) => Shots.Add(numPins);

	// Cached repeatable objects
	private static readonly IList<int> EmptyList = new List<int>().AsReadOnly();
	private static readonly IList<int> StrikeList = new List<int>() { 10 }.AsReadOnly();
}
