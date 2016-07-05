using SQLite;

namespace Remember_It {
	/// <summary>
	/// CREATE TABLE Baralhos (
	///		ID int PRIMARY KEY AUTO_INCREMENT,
	///		Tema VARCHAR(100) NOT NULL,
	///		Titulo VARCHAR(100) NOT NULL,
	///		Autor VARCHAR(100) NOT NULL,
	///		Cartas VARCHAR(256) NOT NULL,
	///		NCartas int NOT NULL,
	///		Partidas int NOT NULL,
	///		Acertos int NOT NULL,
	///		Erros int NOT NULL,
	///		Favorito bool NULL
	///	);
	/// </summary>
	public class Baralhos {
		public Baralhos () {
			ID = -1;
			Tema = "";
			Titulo = "";
			Autor = "";
			Cartas = "";
			NCartas = 0;
			Partidas = 0;
			Acertos = 0;
			Erros = 0;
			Favorito = false;
		}

		public Baralhos (string cartas) {
			ID = -1;
			Tema = "";
			Titulo = "";
			Autor = "";
			Cartas = cartas;
			NCartas = 0;
			Partidas = 0;
			Acertos = 0;
			Erros = 0;
			Favorito = false;
		}

		public Baralhos (string tema, string titulo, string autor, string cartas, int nCartas, int partidas, int acertos, int erros, bool favorito) {
			ID = -1;
			Tema = tema;
			Titulo = titulo;
			Autor = autor;
			Cartas = cartas;
			NCartas = nCartas;
			Partidas = partidas;
			Acertos = acertos;
			Erros = erros;
			Favorito = favorito;
		}

		public Baralhos (string tema, string titulo, string autor, string cartas) {
			ID = -1;
			Tema = tema;
			Titulo = titulo;
			Autor = autor;
			Cartas = cartas;
			NCartas = 0;
			Partidas = 0;
			Acertos = 0;
			Erros = 0;
			Favorito = false;
		}

		[PrimaryKey, AutoIncrement]
		public int ID { get; set; }

		[NotNull]
		public string Tema { get; set; }

		[NotNull]
		public string Titulo { get; set; }

		[NotNull]
		public string Autor { get; set; }

		[NotNull]
		public string Cartas { get; set; }

		[NotNull]
		public int NCartas { get; set; }

		[NotNull]
		public int Partidas { get; set; }

		[NotNull]
		public int Acertos { get; set; }

		[NotNull]
		public int Erros { get; set; }

		[NotNull]
		public bool Favorito { get; set; }
	}
}
 