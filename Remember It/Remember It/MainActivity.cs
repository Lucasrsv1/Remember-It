using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Widget;

namespace Remember_It {
	[Activity(Label = "@string/ApplicationName", ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait, MainLauncher = true, Icon = "@drawable/icon", Theme = "@style/CustomActionBarTheme")]
	public class MainActivity : Activity {
		private static bool isPack = true;
		private bool editing;
		private string appPath;
		private string choosedTheme;
		private GridLayout packsGrid;

		protected override void OnCreate (Bundle bundle) {
			base.OnCreate(bundle);

			// Set our view from the "main" layout resource
			SetContentView(Resource.Layout.Main);
			Title = GetString(Resource.String.ApplicationName) + " (" + GetString(Resource.String.PlayS) + ")";

			editing = false;
			choosedTheme = "";
			appPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
			packsGrid = FindViewById<GridLayout>(Resource.Id.packs);

			//CREATE DATABASE IF NOT EXISTS RememberIt;
			try {
				SGBD.CreateDatabase();
			} catch {
				Toast.MakeText(this, "Não foi possível criar o banco de dados.", ToastLength.Long).Show();
			}

			FindViewById<Button>(Resource.Id.Criar).Click += CriarBaralho;
			FindViewById<Button>(Resource.Id.Editar).Click += EditarBaralho;
			FindViewById<Button>(Resource.Id.Sair).Click += delegate { Process.KillProcess(Process.MyPid()); };

			//Add themes or packs
			AddButons();
		}

		public void AddButons () {
			packsGrid.RemoveAllViews();

			if (isPack) {
				Dictionary<int, string> packs = SGBD.AcessarNomesBaralhos();

				foreach (KeyValuePair<int, string> pack in packs) {
					AddButton(pack.Value, pack.Key.ToString(), false);
				}
			} else if (choosedTheme.Length == 0) {
				string[] buttonsLabels = SGBD.AcessarTemas().ToArray();

				foreach (string theme in buttonsLabels) {
					AddButton(theme, "", true);
				}
			} else {
				Dictionary<int, string> packs = SGBD.AcessarBaralhosPorTema(choosedTheme);

				if (packs.Count == 0) {
					choosedTheme = "";
					if (editing)
						Title = GetString(Resource.String.ApplicationName) + " (" + GetString(Resource.String.EditarS) + ")";
					else
						Title = GetString(Resource.String.ApplicationName) + " (" + GetString(Resource.String.PlayS) + ")";

					AddButons();
				}

				foreach (KeyValuePair<int, string> pack in packs) {
					AddButton(pack.Value, pack.Key.ToString(), false);
				}
			}
		}

		public void AddButton (string text, string tag, bool isTheme) {
			Button newButton = new Button(packsGrid.Context);
			GridLayout.LayoutParams layout = new GridLayout.LayoutParams();
			layout.Width = DipToPx(165);
			layout.Height = DipToPx(95);

			newButton.SetBackgroundColor(new Color(243, 229, 245));
			newButton.SetTextColor(new Color(74, 20, 140));
			newButton.SetTextSize(Android.Util.ComplexUnitType.Dip, 21);
			newButton.SetTypeface(Typeface.SansSerif, TypefaceStyle.Normal);
			newButton.Gravity = GravityFlags.Center;
			newButton.Text = text;

			if (packsGrid.ChildCount % 2 != 0)
				layout.LeftMargin = DipToPx(10);
			else if (packsGrid.ChildCount != 0)
				layout.TopMargin = DipToPx(10);

			newButton.LayoutParameters = layout;
			newButton.SetTag(Resource.Id.baralhoId, tag);

			if (isTheme)
				newButton.Click += ChooseTheme;
			else
				newButton.Click += OpenPack;

			packsGrid.RowCount = (int) Math.Ceiling((packsGrid.ChildCount + 1) / 2f) + 1;
			packsGrid.AddView(newButton);
		}

		public void ChooseTheme (object sender, EventArgs e) {
			choosedTheme = ((Button) sender).Text;
			if (editing)
				Title = choosedTheme + " (" + GetString(Resource.String.EditarS) + ")";
			else
				Title = choosedTheme + " (" + GetString(Resource.String.PlayS) + ")";

			AddButons();
		}

		public void OpenPack (object sender, EventArgs e) {
			int id;

			if (editing) {
				try {
					id = int.Parse(((Button) sender).GetTag(Resource.Id.baralhoId).ToString());

					Intent intent = new Intent(this, typeof(EditorBaralho));
					intent.PutExtra("Baralho_ID", id);
					StartActivity(intent);
				} catch {
					Toast.MakeText(this, "Erro ao abrir o baralho.", ToastLength.Short).Show();
				}
			} else {
				try {
					id = int.Parse(((Button) sender).GetTag(Resource.Id.baralhoId).ToString());

					Toast.MakeText(this, "Play with Baralho_ID: " + ((Button) sender).GetTag(Resource.Id.baralhoId).ToString(), ToastLength.Short).Show();
				} catch {
					Toast.MakeText(this, "Erro ao abrir o baralho.", ToastLength.Short).Show();
				}
			}
		}

		public override void OnBackPressed () {
			if (choosedTheme != "") {
				choosedTheme = "";
				if (editing)
					Title = GetString(Resource.String.ApplicationName) + " (" + GetString(Resource.String.EditarS) + ")";
				else
					Title = GetString(Resource.String.ApplicationName) + " (" + GetString(Resource.String.PlayS) + ")";

				AddButons();
			}
		}

		protected override void OnRestart () {
			base.OnRestart();

			//Atualizar cartas ou temas
			AddButons();
		}

		protected override void OnDestroy () {
			SGBD.CloseConnection();

			base.OnDestroy();
		}

		public int DipToPx (int dp) {
			return (int) Android.Util.TypedValue.ApplyDimension(Android.Util.ComplexUnitType.Dip, dp, Resources.DisplayMetrics);
		}

		public int PxToDip (int px) {
			return (int) (px / Resources.DisplayMetrics.Density);
		}

		public void CriarBaralho (object sender, EventArgs e) {
			string fileDir = System.IO.Path.Combine(appPath, "Baralhos.length");
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

			Baralhos baralho = new Baralhos("Indefinido", "Novo Baralho", "", baralhoFileName);
			SGBD.AdicionarBaralho(baralho);

			Directory.CreateDirectory(System.IO.Path.Combine(appPath, baralhoFileName.Replace(".lrv", " Resources")));

			using (XmlWriter xmlW = XmlWriter.Create(System.IO.Path.Combine(appPath, baralhoFileName))) {
				xmlW.WriteStartDocument();
				xmlW.WriteStartElement("Cartas");

				xmlW.WriteElementString("Next", "0");

				xmlW.WriteEndElement();
				xmlW.WriteEndDocument();
				xmlW.Close();
			}

			Intent intent = new Intent(this, typeof(EditorBaralho));
			intent.PutExtra("Baralho_ID", baralho.ID);
			StartActivity(intent);
		}

		public void EditarBaralho (object sender, EventArgs e) {
			//Change to "choose for edit" mode.
			if (editing) {
				editing = false;
				((Button) sender).Text = GetString(Resource.String.Editar);
				Title = GetString(Resource.String.ApplicationName) + " (" + GetString(Resource.String.PlayS) + ")";
				Toast.MakeText(this, "Escolha um baralho para praticar.", ToastLength.Short).Show();
			} else {
				editing = true;
				((Button) sender).Text = GetString(Resource.String.Play);
				Title = GetString(Resource.String.ApplicationName) + " (" + GetString(Resource.String.EditarS) + ")";
				Toast.MakeText(this, "Escolha um baralho para editar.", ToastLength.Short).Show();
			}
		}

		public override bool OnCreateOptionsMenu (IMenu menu) {
			MenuInflater.Inflate(Resource.Menu.MainActivityMenu, menu);
			return base.OnCreateOptionsMenu(menu);
		}

		public override bool OnOptionsItemSelected (IMenuItem item) {
			switch (item.ItemId) {
				case Resource.Id.ThemeGroup:
					isPack = false;
					choosedTheme = "";
					if (editing)
						Title = GetString(Resource.String.ApplicationName) + " (" + GetString(Resource.String.EditarS) + ")";
					else
						Title = GetString(Resource.String.ApplicationName) + " (" + GetString(Resource.String.PlayS) + ")";

					AddButons();
					break;
				case Resource.Id.PackGroup:
					isPack = true;
					choosedTheme = "";
					if (editing)
						Title = GetString(Resource.String.ApplicationName) + " (" + GetString(Resource.String.EditarS) + ")";
					else
						Title = GetString(Resource.String.ApplicationName) + " (" + GetString(Resource.String.PlayS) + ")";

					AddButons();
					break;
				default:
					return base.OnOptionsItemSelected(item);
			}

			return true;
		}
	}
}

