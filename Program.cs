using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ScoreCounter;

class Program {
	const int MAX_PLAYERS = 5;
	private static readonly ConsoleColor[] PlayerColors = new[] { ConsoleColor.Cyan, ConsoleColor.Yellow, ConsoleColor.Red, ConsoleColor.Green, ConsoleColor.White };
	private static ConsoleColor _defaultColor;

    static void Main(string[] args) {
		// Quick sanity check
		if(PlayerColors.Count() < MAX_PLAYERS) {
			throw new Exception("Invalid configuration. There are less player colors than max players.");
		}

		_defaultColor = Console.ForegroundColor;

		Console.CancelKeyPress += delegate {
			Console.ForegroundColor = _defaultColor;
		};

        GameController game = InitGame();
		GuiDriver gui = new GuiDriver(game);
		gui.Play();
    }

	static GameController InitGame() {
		List<Player> players = new List<Player>(MAX_PLAYERS);

		Console.Clear();
		Console.WriteLine("Welcome to the Bowling ScoreCounter!");
		Console.WriteLine();
		Console.Write($"How many players will be playing? [1-{MAX_PLAYERS}]: ");
		
		char key;

		while((key = Console.ReadKey(true).KeyChar) < '1' || key > (MAX_PLAYERS + '0')) {}

		int numPlayers = key - '0';

		Console.WriteLine(numPlayers);

		for(int playerNum = 1; playerNum <= numPlayers; playerNum++) {
			ConsoleColor currentColor = Console.ForegroundColor;
			Console.ForegroundColor = PlayerColors[playerNum - 1];
			Console.Write($"What is the name of player #{playerNum}? [Player {playerNum}]: ");
			
			string? playerName = Console.ReadLine();
			
			if(String.IsNullOrWhiteSpace(playerName)) {
				playerName = "Player " + playerNum;
			}

			players.Add(new Player(playerName, PlayerColors[playerNum - 1]));

			Console.ForegroundColor = currentColor;
		}

		Console.WriteLine();
		Console.WriteLine($"Starting game with {numPlayers} players: {String.Join(", ", players)}...");
		Console.WriteLine();
		Console.WriteLine("Press any key to continue, and CTRL+C to quit at any time.");
		Console.ReadKey();

		return new GameController(players);
	}
}
