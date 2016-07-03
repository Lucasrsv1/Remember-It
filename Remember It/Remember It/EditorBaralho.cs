using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;

namespace Remember_It {
	[Activity(Label = "@string/Editor", ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait, Theme = "@style/CustomActionBarTheme")]
	public class EditorBaralho : Activity {
		private int baralhoID;
		public bool fav;
		public string appPath;
		public Baralhos baralho;
		public List<string> keys;
		public List<string> cartas;

		protected override void OnCreate (Bundle savedInstanceState) {
			base.OnCreate(savedInstanceState);

			// Create your application here
			appPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);

			SetContentView(Resource.Layout.EditorBaralho);
			ActionBar.SetDisplayHomeAsUpEnabled(true);

			baralhoID = Intent.GetIntExtra("Baralho_ID", -1);
			FindViewById<ListView>(Resource.Id.CartasView).ItemClick += ItemClicked;

			LoadPack();
		}

		public void LoadPack () {
			try {
				baralho = SGBD.AcessarBaralho(baralhoID);

				FindViewById<EditText>(Resource.Id.tema).Text = baralho.Tema;
				FindViewById<EditText>(Resource.Id.titulo).Text = baralho.Titulo;
				FindViewById<EditText>(Resource.Id.autor).Text = baralho.Autor;
				FindViewById<TextView>(Resource.Id.NCartas).Text = GetString(Resource.String.NCartas) + " " + baralho.NCartas.ToString();
				fav = baralho.Favorito;

				keys = new List<string>();
				cartas = new List<string>();
				
				try {
					using (XmlReader xmlR = XmlReader.Create(Path.Combine(appPath, baralho.Cartas))) {
						while (xmlR.Read()) {
							if (xmlR.IsStartElement("Carta")) {
								keys.Add(xmlR["id"]);
								cartas.Add(xmlR["name"]);
							}
						}

						xmlR.Close();
					}
				} catch (Exception err) {
					Toast.MakeText(this, "Erro ao ler as cartas do baralho." + err.Message, ToastLength.Long).Show();
				}
				
				FindViewById<ListView>(Resource.Id.CartasView).Adapter = new CardsManager(cartas, this);
			} catch (Exception err) {
				Toast.MakeText(this, "! " + err.Message, ToastLength.Long).Show();
			}
		}

		public void ItemClicked (object sender, AdapterView.ItemClickEventArgs e) {
			Intent intent = new Intent(this, typeof(EditorCarta));
			intent.PutExtra("Baralho_ID", baralho.ID);
			intent.PutExtra("BaralhoFileName", baralho.Cartas);
			intent.PutExtra("Card_ID", int.Parse(keys[e.Position]));
			intent.PutExtra("Side", "Frente");
			intent.PutExtra("AppPath", appPath);
			StartActivity(intent);
		}

		public XElement NewCard (int nextCard) {
			XElement card = new XElement("Carta");
			card.Add(new XAttribute("id", nextCard));
			card.Add(new XAttribute("name", "Carta Sem Título"));

			XElement front = new XElement("Frente");
			front.Add(new XAttribute("background", Android.Graphics.Color.White.ToArgb()));
			front.Add(new XAttribute("src", ""));
			front.Add(new XElement("Next", 0));

			XElement back = new XElement("Verso");
			back.Add(new XAttribute("background", Android.Graphics.Color.White.ToArgb()));
			back.Add(new XAttribute("src", ""));
			back.Add(new XElement("Next", 0));

			card.Add(front);
			card.Add(back);

			return card;
		}

		protected override void OnRestart () {
			base.OnRestart();

			LoadPack();
		}

		public override bool OnCreateOptionsMenu (IMenu menu) {
			MenuInflater.Inflate(Resource.Menu.EditorBaralhoMenu, menu);
			return base.OnCreateOptionsMenu(menu);
		}

		public override bool OnOptionsItemSelected (IMenuItem item) {
			switch (item.ItemId) {
				case Android.Resource.Id.Home:
					Finish();
					break;
				case Resource.Id.NovaCarta:
					try {
						string xmlDir = Path.Combine(appPath, baralho.Cartas);
						XDocument cartasXML = XDocument.Load(xmlDir);

						int nextCard = -1;
						int.TryParse(cartasXML.Element("Cartas").Element("Next").Value, out nextCard);

						cartasXML.Element("Cartas").Add(NewCard(nextCard));
						cartasXML.Element("Cartas").Element("Next").Value = (nextCard + 1).ToString();
						cartasXML.Save(xmlDir);

						Intent intent = new Intent(this, typeof(EditorCarta));
						intent.PutExtra("Baralho_ID", baralho.ID);
						intent.PutExtra("BaralhoFileName", baralho.Cartas);
						intent.PutExtra("Card_ID", nextCard);
						intent.PutExtra("Side", "Frente");
						intent.PutExtra("AppPath", appPath);
						StartActivity(intent);

						baralho.NCartas++;
						SGBD.UpdateBaralho(baralho);
						Toast.MakeText(this, "Carta adicionada ao baralho.", ToastLength.Long).Show();
					} catch (Exception err) {
						Toast.MakeText(this, "3! " + err.Message, ToastLength.Long).Show();
					}
					break;
				case Resource.Id.SalvarBaralho:
					try {
						baralho.Tema = FindViewById<EditText>(Resource.Id.tema).Text;
						baralho.Titulo = FindViewById<EditText>(Resource.Id.titulo).Text;
						baralho.Autor = FindViewById<EditText>(Resource.Id.autor).Text;
						baralho.Favorito = fav;

						SGBD.UpdateBaralho(baralho);
						Toast.MakeText(this, "Baralho salvo com sucesso!", ToastLength.Long).Show();
					} catch (Exception err) {
						Toast.MakeText(this, "2! " + err.Message, ToastLength.Long).Show();
					}
					break;
				case Resource.Id.ExcluirBaralho:
					try {
						Directory.Delete(Path.Combine(appPath, baralho.Cartas.Replace(".lrv", " Resources")), true);

						string filePath = Path.Combine(appPath, baralho.Cartas);
						File.Delete(filePath);
						SGBD.DeleteBaralho(baralho.ID);

						Toast.MakeText(this, "Baralho excluído com sucesso.", ToastLength.Long).Show();
						Finish();
					} catch {
						Toast.MakeText(this, "Erro ao excluir o baralho.", ToastLength.Long).Show();
					}
					break;
				default:
					return base.OnOptionsItemSelected(item);
			}

			return true;
		}
	}
}