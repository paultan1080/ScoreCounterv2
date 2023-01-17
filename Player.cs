namespace ScoreCounter;

public record Player(string Name, ConsoleColor Color) {
	public override string ToString() => Name;
};
