using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace Remember_It {
	public struct Rect {
		public int x;
		public int y;
		public int width;
		public int height;

		public Rect (int x, int y, int width, int height) {
			this.x = x;
			this.y = y;
			this.width = width;
			this.height = height;
		}

		public Rect (string cordinates) {
			string[] splitted = cordinates.Split(',');

			int.TryParse(splitted[0], out x);
			int.TryParse(splitted[1], out y);
			int.TryParse(splitted[2], out width);
			int.TryParse(splitted[3], out height);
		}

		public override string ToString () {
			return string.Format("{0},{1},{2},{3}", x, y, width, height);
		}
	}

	[Activity(Label = "@string/EditorCartas")]
	public class EditorCarta : Activity {
		public int cardID;
		public bool isImage;
		public string side;
		public string xmlDir;
		public string appPath;
		public string baralhoFileName;
		public View selected;
		public EditText title;
		public GridLayout tools;
		public RelativeLayout carta;
		public RelativeLayout relativeLayout;

		protected override void OnCreate (Bundle savedInstanceState) {
			base.OnCreate(savedInstanceState);

			// Create your application here
			SetContentView(Resource.Layout.EditorCarta);

			ActionBar.SetDisplayHomeAsUpEnabled(true);

			cardID = Intent.GetIntExtra("Card_ID", -1);
			side = Intent.GetStringExtra("Side");
			appPath = Intent.GetStringExtra("AppPath");
			baralhoFileName = Intent.GetStringExtra("BaralhoFileName");
			xmlDir = System.IO.Path.Combine(appPath, baralhoFileName);
			
			carta = FindViewById<RelativeLayout>(Resource.Id.carta);
			title = FindViewById<EditText>(Resource.Id.cartaTitulo);
			relativeLayout = FindViewById<RelativeLayout>(Resource.Id.relativeLayout1);

			carta.Click += Unselect;
			relativeLayout.Click += Unselect;
			FindViewById<LinearLayout>(Resource.Id.linearLayout0).Click += Unselect;

			using (XmlReader xmlR = XmlReader.Create(xmlDir)) {
				while (xmlR.Read()) {
					//Toast.MakeText(this, xmlR.NodeType.ToString() + ": " + xmlR.Name, ToastLength.Short).Show();
					if (xmlR.IsStartElement("Carta")) {
						if (xmlR["id"] == cardID.ToString()) {
							string nome = xmlR["name"];
							title.Text = nome;

							while (!xmlR.IsStartElement(side))
								xmlR.Read();

							if (xmlR.IsStartElement(side)) {
								string background = xmlR["background"];

								if (background.Contains(".")) {
									Bitmap image;
									try {
										image = BitmapFactory.DecodeFile(System.IO.Path.Combine(appPath, baralhoFileName.Replace(".lrv", " Resources"), background));
									} catch {
										image = null;
									}

									if (image != null) {
										carta.Background = new BitmapDrawable(image);
									} else {
										Toast.MakeText(this, background + " não encontrado.", ToastLength.Long).Show();
									}
								} else {
									Toast.MakeText(this, "Bg: " + background, ToastLength.Long).Show();
									carta.SetBackgroundColor(new Color(int.Parse(background)));
								}

								bool end = false;
								while (xmlR.Read() && !end) {
									//Criar elementos.
									if (xmlR.IsStartElement()) {
										switch (xmlR.Name) {
											case "Text":
												try {
													EditText newLabel = new EditText(carta.Context);

													//Lê as configurações dos atributos do Texto no XML
													int fontS = 12;
													int.TryParse(xmlR["fontSize"], out fontS);
													string text = xmlR.Value;
													string style = xmlR["style"];
													string gravity = xmlR["gravity"];
													Rect rectT = new Rect(xmlR["rect"]);
													Color color = new Color(int.Parse(xmlR["color"]));
													Color backgroundT = new Color(int.Parse(xmlR["background"]));

													//Cria as configurações de tamanho e posicionamento do Texto
													RelativeLayout.LayoutParams layoutT = new RelativeLayout.LayoutParams(DipToPx(rectT.width), DipToPx(rectT.height));
													layoutT.LeftMargin = DipToPx(rectT.x);
													layoutT.TopMargin = DipToPx(rectT.y);

													//Aplica as configurações no novo EditText
													newLabel.SetTextSize(Android.Util.ComplexUnitType.Dip, fontS);
													newLabel.SetText(text, TextView.BufferType.Editable);
													newLabel.SetTypeface(null, GetTypeFace(style));
													newLabel.Gravity = GetGravity(gravity);
													newLabel.LayoutParameters = layoutT;
													newLabel.SetTextColor(color);
													newLabel.SetBackgroundColor(backgroundT);
													newLabel.Click += SelectT;
													newLabel.FocusChange += Focus;

													carta.AddView(newLabel);
												} catch (Exception err) {
													Toast.MakeText(this, "Erro ao criar um texto: " + err.Message, ToastLength.Long).Show();
												}
												break;
											case "Image":
												try {
													ImageView newImage = new ImageView(carta.Context);

													//Lê as configurações dos atributos da Imagem no XML
													string src = xmlR["src"];
													Bitmap image;
													try {
														image = BitmapFactory.DecodeFile(System.IO.Path.Combine(appPath, baralhoFileName.Replace(".lrv", " Resources"), src));
													} catch {
														image = null;
													}
													string scale = xmlR["scale"];
													Rect rectI = new Rect(xmlR["rect"]);
													Color backgroundI = new Color(int.Parse(xmlR["background"]));

													//Cria as configurações de tamanho e posicionamento da Imagem
													RelativeLayout.LayoutParams layoutI = new RelativeLayout.LayoutParams(DipToPx(rectI.width), DipToPx(rectI.height));
													layoutI.LeftMargin = DipToPx(rectI.x);
													layoutI.TopMargin = DipToPx(rectI.y);

													//Aplica as configurações no novo ImageView
													if (image != null) {
														newImage.SetImageBitmap(image);
													} else {
														Toast.MakeText(this, src + " não encontrado.", ToastLength.Long).Show();
														newImage.SetImageResource(Android.Resource.Color.Transparent);
													}
													newImage.SetScaleType(ImageView.ScaleType.ValueOf(scale));
													newImage.LayoutParameters = layoutI;
													newImage.SetBackgroundColor(backgroundI);
													newImage.Click += SelectI;
													newImage.FocusChange += Focus;

													carta.AddView(newImage);
												} catch (Exception err) {
													Toast.MakeText(this, "Erro ao criar uma Imagem: " + err.Message, ToastLength.Long).Show();
												}
												break;
											case "Carta":
												end = true;
												break;
										}
									}
								}

								break;
							}
						}
					}
				}
			}

			Toast.MakeText(this, "Carregamento concluído!", ToastLength.Short).Show();
		}

		public void AddText (object sender, EventArgs e) {
			EditText newLabel = new EditText(carta.Context);

			//Cria as configurações de tamanho e posicionamento do Texto
			RelativeLayout.LayoutParams layoutT = new RelativeLayout.LayoutParams(DipToPx(350), RelativeLayout.LayoutParams.WrapContent);
			layoutT.LeftMargin = 0;
			layoutT.TopMargin = DipToPx(152);

			//Aplica as configurações no novo EditText
			newLabel.SetTextSize(Android.Util.ComplexUnitType.Dip, 22);
			newLabel.SetText("Novo Texto", TextView.BufferType.Editable);
			newLabel.SetTypeface(null, TypefaceStyle.Normal);
			newLabel.Gravity = GravityFlags.Center;
			newLabel.LayoutParameters = layoutT;
			newLabel.SetTextColor(Color.Black);
			newLabel.SetBackgroundColor(Color.Transparent);
			newLabel.Click += SelectT;
			newLabel.FocusChange += Focus;

			carta.AddView(newLabel);
			SelectT(newLabel, null);

			Toast.MakeText(this, "Adicionado", ToastLength.Short).Show();
		}

		public void AddImg (object sender, EventArgs e) {

		}

		public void BackgroundColor (object sender, EventArgs e) {

		}

		public void Turn (object sender, EventArgs e) {

		}

		public void SelectT (object sender, EventArgs e = null) {
			EditText clicked = (EditText)sender;

			selected = clicked;
			selected.RequestFocus();

			isImage = false;
			InvalidateOptionsMenu();
		}

		public void SelectI (object sender, EventArgs e = null) {
			ImageView clicked = (ImageView)sender;

			selected = clicked;
			selected.RequestFocus();

			isImage = true;
			InvalidateOptionsMenu();
		}

		public void Unselect (object sender = null, EventArgs e = null) {
			if (selected != null) {
				selected.ClearFocus();
				selected = null;

				title.RequestFocus();
				isImage = false;
				InvalidateOptionsMenu();
			}
		}

		public void Focus (object sender, View.FocusChangeEventArgs e) {
			if (e.HasFocus) {
				SelectT(sender);
			} else if ((object)selected == sender) {
				Unselect();
			}
		}

		public int DipToPx (int dp) {
			return (int) Android.Util.TypedValue.ApplyDimension(Android.Util.ComplexUnitType.Dip, dp, Resources.DisplayMetrics);
		}

		public TypefaceStyle GetTypeFace (string style) {
			switch (style.ToLower()) {
				case "bold|italic":
					return TypefaceStyle.BoldItalic;
				case "bold":
					return TypefaceStyle.Bold;
				case "italic":
					return TypefaceStyle.Italic;
				default:
					return TypefaceStyle.Normal;
			}
		}

		public GravityFlags GetGravity (string gravity) {
			string[] flags = gravity.Split('|');
			GravityFlags[] gravityFlags = new GravityFlags[flags.Length];

			for (int i = 0; i < flags.Length; i++) {
				switch (flags[i]) {
					case "Left":
						gravityFlags[i] = GravityFlags.Left;
						break;
					case "CenterHorizontal":
						gravityFlags[i] = GravityFlags.CenterHorizontal;
						break;
					case "Right":
						gravityFlags[i] = GravityFlags.Right;
						break;
					case "Top":
						gravityFlags[i] = GravityFlags.Top;
						break;
					case "CenterVertical":
						gravityFlags[i] = GravityFlags.CenterVertical;
						break;
					case "Bottom":
						gravityFlags[i] = GravityFlags.Bottom;
						break;
					case "Center":
						gravityFlags[i] = GravityFlags.Center;
						break;
				}
			}

			if (gravityFlags.Length > 1)
				return gravityFlags[0] | gravityFlags[1];
			else
				return gravityFlags[0];
		}

		public override bool OnCreateOptionsMenu (IMenu menu) {
			if (selected == null) {
				MenuInflater.Inflate(Resource.Menu.EditorCartaMenu, menu);

				if (relativeLayout == null)
					relativeLayout = FindViewById<RelativeLayout>(Resource.Id.relativeLayout1);

				relativeLayout.RemoveView(FindViewById<GridLayout>(Resource.Id.gridLayout1));
				LayoutInflater.Inflate(Resource.Layout.DefaultTools, relativeLayout);

				if (side == "Frente")
					FindViewById<Button>(Resource.Id.EditarVerso).Text = "Editar Verso";
				else
					FindViewById<Button>(Resource.Id.EditarVerso).Text = "Editar Frente";

				FindViewById<Button>(Resource.Id.AddText).Click += AddText;
				FindViewById<Button>(Resource.Id.AddImg).Click += AddImg;
				FindViewById<Button>(Resource.Id.BackgroundColor).Click += BackgroundColor;
				FindViewById<Button>(Resource.Id.EditarVerso).Click += Turn;
			} else if (isImage) {
				MenuInflater.Inflate(Resource.Menu.ImageSelected, menu);

				if (relativeLayout == null)
					relativeLayout = FindViewById<RelativeLayout>(Resource.Id.relativeLayout1);

				//relativeLayout.RemoveView(FindViewById<GridLayout>(Resource.Id.gridLayout1));
				//LayoutInflater.Inflate(Resource.Layout.ImageTools, relativeLayout);
			} else {
				MenuInflater.Inflate(Resource.Menu.LabelSelected, menu);

				if (relativeLayout == null)
					relativeLayout = FindViewById<RelativeLayout>(Resource.Id.relativeLayout1);

				//relativeLayout.RemoveView(FindViewById<GridLayout>(Resource.Id.gridLayout1));
				//LayoutInflater.Inflate(Resource.Layout.LabelTools, relativeLayout);
			}

			return base.OnCreateOptionsMenu(menu);
		}

		public override bool OnOptionsItemSelected (IMenuItem item) {
			switch (item.ItemId) {
				case Android.Resource.Id.Home:
					Finish();
					break;
				case Resource.Id.RemoveView:
					carta.RemoveView(selected);
					Unselect();
					break;
				default:
					return base.OnOptionsItemSelected(item);
			}

			return true;
		}
	}
}