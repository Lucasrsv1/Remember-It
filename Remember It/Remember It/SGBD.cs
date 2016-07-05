using System.Collections.Generic;
using System.IO;
using System.Linq;
using SQLite;

namespace Remember_It {
	/// <summary>
	/// A classe a seguir deve conter todos os m�todos relacionados ao acesso a base de dados.
	/// OBS.: a classe n�o deve ser instanciada e seus m�todos s�o sempre est�ticos.
	/// </summary>
	static class SGBD {
		private static SQLiteConnection connection = Connect();

		public static SQLiteConnection Connect () {
			//USE databasePath COMO localhost
			string databasePath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);

			//CREATE DATABASE IF NOT EXISTS RememberIt.sqlite;
			//USE RememberIt.sqlite;
			SQLiteConnection conn = new SQLiteConnection(Path.Combine(databasePath, "RememberIt.sqlite"));

			return conn;
		}

		public static void CloseConnection () {
			connection.Close();
		}

		public static void CreateDatabase () {
			//CREATE TABLE IF NOT EXISTS Baralhos;
			connection.CreateTable<Baralhos>();
		}

		public static int AdicionarBaralho (Baralhos baralho) {
			return connection.Insert(baralho);
		}

		public static Baralhos AcessarBaralho (int id) {
			System.Collections.Generic.List<Baralhos> baralho = connection.Query<Baralhos>("SELECT * FROM Baralhos WHERE ID = ?", id);
			if (baralho.Count > 0)
				return baralho[0];
			else
				return null;

		}

		public static System.Collections.Generic.List<Baralhos> AcessarBaralho () {
			return connection.Query<Baralhos>(string.Format("SELECT * FROM Baralhos"));
		}

		public static void UpdateBaralho (Baralhos baralho) {
			connection.Update(baralho);
		}

		public static void DeleteBaralho (Baralhos baralho) {
			connection.Delete(baralho);
		}

		public static void DeleteBaralho (object baralho_ID) {
			connection.Delete<Baralhos>(baralho_ID);
		}

		public static List<string> AcessarTemas () {
			return (from tema in connection.Query<Baralhos>("SELECT DISTINCT Tema FROM Baralhos")
					select tema.Tema).ToList();
		}

		public static Dictionary<int, string> AcessarNomesBaralhos () {
			Dictionary<int, string> assoc = new Dictionary<int, string>();

			foreach (Baralhos baralho in (from bar in connection.Query<Baralhos>("SELECT * FROM Baralhos") select bar)) {
				assoc.Add(baralho.ID, baralho.Titulo);
			}

			return assoc;
		}

		public static Dictionary<int, string> AcessarBaralhosPorTema (string theme) {
			Dictionary<int, string> assoc = new Dictionary<int, string>();

			foreach (Baralhos baralho in (from bar in connection.Query<Baralhos>("SELECT * FROM Baralhos WHERE Tema = ?", theme) select bar)) {
				assoc.Add(baralho.ID, baralho.Titulo);
			}

			return assoc;
		}
	}
}