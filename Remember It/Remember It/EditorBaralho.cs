using System;
using System.IO;
using System.Xml;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace Remember_It {
	[Activity(Label = "Editor de Baralhos")]
	public class EditorBaralho : Activity {
		public bool fav;
		public string appPath;
		public Baralhos baralho;

		protected override void OnCreate (Bundle savedInstanceState) {
			base.OnCreate(savedInstanceState);

			// Create your application here
			SetContentView(Resource.Layout.EditorBaralho);

			ActionBar.SetDisplayHomeAsUpEnabled(true);

			appPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);

			try {
				baralho = SGBD.AcessarBaralho(Intent.GetIntExtra("Baralho_ID", -1));

				FindViewById<EditText>(Resource.Id.tema).Text = baralho.Tema;
				FindViewById<EditText>(Resource.Id.titulo).Text = baralho.Titulo;
				FindViewById<EditText>(Resource.Id.autor).Text = baralho.Autor;
				fav = baralho.Favorito;
			} catch (Exception err) {
				Toast.MakeText(this, "! " + err.Message, ToastLength.Long).Show();
			}
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