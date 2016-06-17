using System;
using System.IO;
using System.Xml;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

namespace Remember_It {
	[Activity(Label = "Remember It", MainLauncher = true, Icon = "@drawable/icon")]
	public class MainActivity : Activity {
		public string appPath;
		public Baralhos baralho;

		protected override void OnCreate (Bundle bundle) {
			base.OnCreate(bundle);

			// Set our view from the "main" layout resource
			SetContentView(Resource.Layout.Main);

			appPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);

			//CREATE DATABASE IF NOT EXISTS RememberIt;
			try {
				SGBD.CreateDatabase();
			} catch {
				Toast.MakeText(this, "Não foi possível criar o banco de dados.", ToastLength.Long).Show();
			}

			FindViewById<Button>(Resource.Id.Criar).Click += CriarBaralho;
			FindViewById<Button>(Resource.Id.Editar).Click += EditarBaralho;
			FindViewById<Button>(Resource.Id.Sair).Click += delegate { Process.KillProcess(Process.MyPid()); };
		}

		protected override void OnDestroy () {
			SGBD.CloseConnection();

			base.OnDestroy();
		}

		public void CriarBaralho (object sender, EventArgs e) {
			string fileDir = Path.Combine(appPath, "Baralhos.length");
			int length;

			if (File.Exists(fileDir)) {
				StreamReader sr = new StreamReader(fileDir);
				int.TryParse(sr.ReadToEnd(), out length);
				sr.Close();

				StreamWriter sw = new StreamWriter(fileDir);
				sw.Write(length + 1);
				sw.Close();
			} else {
				length = 0;
				StreamWriter sw = File.CreateText(fileDir);
				sw.Write(1);
				sw.Close();
			}

			string baralhoFileName = "Baralho-" + length.ToString() + ".lrv";
			Toast.MakeText(this, baralhoFileName, ToastLength.Long).Show();

			baralho = new Baralhos(baralhoFileName);
			SGBD.AdicionarBaralho(baralho);

			using (XmlWriter xmlW = XmlWriter.Create(Path.Combine(appPath, baralhoFileName))) {
				xmlW.WriteStartDocument();
				xmlW.WriteStartElement("Cartas");

				xmlW.WriteElementString("Length", "0");

				xmlW.WriteEndElement();
				xmlW.WriteEndDocument();
				xmlW.Close();

				Toast.MakeText(this, "XML Criado!", ToastLength.Long).Show();
			}

			Toast.MakeText(this, appPath, ToastLength.Long).Show();
			Intent intent = new Intent(this, typeof(EditorBaralho));
			intent.PutExtra("Baralho_ID", baralho.ID);
			StartActivity(intent);
		}

		public void EditarBaralho (object sender, EventArgs e) {
			//Change to "choose for edit" mode.
			Toast.MakeText(this, "Not implemented functionality.", ToastLength.Long).Show();
		}
	}
}

