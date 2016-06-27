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
using Android.Text;
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

	[Activity(Label = "@string/EditorCartas", ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait)]
	public class EditorCarta : Activity, IDialogInterfaceOnClickListener {
		public int cardID;
		public bool isImage;
		public string side;
		public string xmlDir;
		public string appPath;
		public string baralhoFileName;
		public string[] aligns;
		public string[] scales;
		public View selected;
		public EditText title;
		public GridLayout tools;
		public RelativeLayout carta;
		public RelativeLayout relativeLayout;

		public int request;
		public bool shown;
		public bool shown2;
		public Color originalColor;
		public RelativeLayout.LayoutParams originalLayout;

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

			Title = GetString(Resource.String.EditorCartas) + " (" + ((side == "Frente") ? GetString(Resource.String.Frente) : GetString(Resource.String.Verso)) + ")";

			carta = FindViewById<RelativeLayout>(Resource.Id.carta);
			title = FindViewById<EditText>(Resource.Id.cartaTitulo);
			relativeLayout = FindViewById<RelativeLayout>(Resource.Id.relativeLayout1);

			carta.Click += Unselect;
			relativeLayout.Click += Unselect;
			FindViewById<LinearLayout>(Resource.Id.linearLayout0).Click += Unselect;
			FindViewById<EditText>(Resource.Id.cartaTitulo).FocusChange += TitleFocus;

			using (XmlReader xmlR = XmlReader.Create(xmlDir)) {
				while (xmlR.Read()) {
					if (xmlR.IsStartElement("Carta")) {
						if (xmlR["id"] == cardID.ToString()) {
							string nome = xmlR["name"];
							title.Text = nome;

							while (!xmlR.IsStartElement(side))
								xmlR.Read();

							if (xmlR.IsStartElement(side)) {
								string cardSrc = xmlR["src"];

								if (cardSrc.Length > 0) {
									Bitmap image;
									try {
										image = BitmapFactory.DecodeFile(System.IO.Path.Combine(appPath, baralhoFileName.Replace(".lrv", " Resources"), cardSrc));
									} catch {
										image = null;
									}

									if (image != null) {
										carta.Background = new BitmapDrawable(image);
									} else {
										Toast.MakeText(this, cardSrc + " não encontrado.", ToastLength.Long).Show();
									}
								}

								string background = xmlR["background"];
								if (background.Length > 0)
									carta.SetBackgroundColor(new Color(int.Parse(background)));
								else
									carta.SetBackgroundColor(Color.White);

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
													int zIndex = int.Parse(xmlR["z"]);

													//Cria as configurações de tamanho e posicionamento do Texto
													RelativeLayout.LayoutParams layoutT = new RelativeLayout.LayoutParams(DipToPx(rectT.width), DipToPx(rectT.height));
													layoutT.LeftMargin = DipToPx(rectT.x);
													layoutT.TopMargin = DipToPx(rectT.y);

													//Aplica as configurações no novo EditText
													newLabel.SetTextSize(Android.Util.ComplexUnitType.Dip, fontS);
													newLabel.SetText(text, TextView.BufferType.Editable);
													newLabel.SetTypeface(newLabel.Typeface, GetTypeFace(style));
													newLabel.Gravity = GetGravity(gravity);
													newLabel.LayoutParameters = layoutT;
													newLabel.SetTextColor(color);
													newLabel.SetBackgroundColor(backgroundT);
													newLabel.SetZ(zIndex);
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
													int zIndex = int.Parse(xmlR["z"]);

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
													newImage.SetZ(zIndex);
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
		}

		public void AddText (object sender, EventArgs e) {
			EditText newLabel = new EditText(carta.Context);

			//Cria as configurações de tamanho e posicionamento do Texto
			RelativeLayout.LayoutParams layoutT = new RelativeLayout.LayoutParams(RelativeLayout.LayoutParams.WrapContent, RelativeLayout.LayoutParams.WrapContent);
			layoutT.LeftMargin = DipToPx(104);
			layoutT.TopMargin = DipToPx(152);

			//Aplica as configurações no novo EditText
			newLabel.SetTextSize(Android.Util.ComplexUnitType.Dip, 22);
			newLabel.SetText("Novo Texto", TextView.BufferType.Editable);
			newLabel.SetTypeface(newLabel.Typeface, TypefaceStyle.Normal);
			newLabel.Gravity = GravityFlags.Center;
			newLabel.LayoutParameters = layoutT;
			newLabel.SetTextColor(Color.Black);
			newLabel.SetBackgroundColor(Color.Transparent);
			newLabel.SetZ(1);
			newLabel.Click += SelectT;
			newLabel.FocusChange += Focus;

			carta.AddView(newLabel);
			SelectT(newLabel, null);
		}

		public void AddImage (Bitmap img) {
			//Adicionar
			ImageView newImage = new ImageView(carta.Context);
			int w, h, max = DipToPx(310);

			if (img.Width > img.Height) {
				w = (img.Width > max) ? max : img.Width;
				h = (int) Math.Round(img.Height * w / (float) img.Width);
			} else {
				h = (img.Height > max) ? max : img.Height;
				w = (int) Math.Round(img.Width * h / (float) img.Height);
			}

			//Cria as configurações de tamanho e posicionamento da Imagem
			RelativeLayout.LayoutParams layoutI = new RelativeLayout.LayoutParams(w, h);
			layoutI.LeftMargin = (int) Math.Round((DipToPx(350) - w) / 2f);
			layoutI.TopMargin = (int) Math.Round((DipToPx(350) - h) / 2f);

			//Aplica as configurações no novo ImageView
			newImage.SetImageBitmap(img);
			newImage.SetScaleType(ImageView.ScaleType.FitXy);
			newImage.LayoutParameters = layoutI;
			newImage.SetBackgroundColor(Color.Transparent);
			newImage.SetZ(0);
			newImage.Click += SelectI;
			newImage.FocusChange += Focus;

			carta.AddView(newImage);
			SelectI(newImage, null);
		}

		public void AddImg (bool change) {
			Intent intent = new Intent(Intent.ActionGetContent);
			intent.SetType("image/*");

			Intent picker = new Intent(Intent.ActionPick, Android.Provider.MediaStore.Images.Media.ExternalContentUri);
			picker.SetType("image/*");

			Intent chooser = Intent.CreateChooser(intent, "Selecione a Imagem");
			chooser.PutExtra(Intent.ExtraInitialIntents, new Intent[] { picker });

			StartActivityForResult(chooser, (change) ? 2 : 1);
		}

		public void HorizontalCenter () {
			if (selected == null)
				return;

			RelativeLayout.LayoutParams layout = new RelativeLayout.LayoutParams((RelativeLayout.LayoutParams) selected.LayoutParameters);
			layout.LeftMargin = (DipToPx(350) - selected.MeasuredWidth) / 2;

			selected.LayoutParameters = layout;
		}

		public void VerticalCenter () {
			if (selected == null)
				return;

			RelativeLayout.LayoutParams layout = new RelativeLayout.LayoutParams((RelativeLayout.LayoutParams) selected.LayoutParameters);
			layout.TopMargin = (DipToPx(350) - selected.MeasuredHeight) / 2;

			selected.LayoutParameters = layout;
		}

		public void Turn (object sender, EventArgs e) {

		}

		public void SelectT (object sender, EventArgs e = null) {
			EditText clicked = (EditText) sender;

			selected = clicked;
			selected.RequestFocus();
			selected.Click -= SelectT;
			selected.Click += SizeEvent;
			selected.LongClick += PosEvent;

			isImage = false;
			InvalidateOptionsMenu();
		}

		public void SelectI (object sender, EventArgs e = null) {
			ImageView clicked = (ImageView) sender;

			selected = clicked;
			selected.RequestFocus();
			selected.Click -= SelectI;
			selected.Click += SizeEvent;
			selected.LongClick += PosEvent;

			isImage = true;
			InvalidateOptionsMenu();
		}

		public void Unselect (object sender = null, EventArgs e = null) {
			if (selected != null) {
				selected.LongClick -= PosEvent;
				selected.Click -= SizeEvent;

				if (isImage)
					selected.Click += SelectI;
				else
					selected.Click += SelectT;

				selected.ClearFocus();
				selected = null;

				title.RequestFocus();
				isImage = false;
				InvalidateOptionsMenu();
			}
		}

		public void PosEvent (object sender, EventArgs e) {
			PositionView();
		}

		public void SizeEvent (object sender, EventArgs e) {
			ResizeView();
		}

		public void Duplicar (object sender = null, EventArgs e = null) {
			if (selected != null) {
				if (isImage) {
					ImageView image = (ImageView) selected;
					ImageView newImage = new ImageView(carta.Context);

					//Cria as configurações de tamanho e posicionamento da Imagem
					RelativeLayout.LayoutParams layoutI = new RelativeLayout.LayoutParams(image.LayoutParameters.Width, image.LayoutParameters.Height);
					layoutI.LeftMargin = ((RelativeLayout.LayoutParams) image.LayoutParameters).LeftMargin + 5;
					layoutI.TopMargin = ((RelativeLayout.LayoutParams) image.LayoutParameters).TopMargin + 5;

					//Copia as configurações para o novo ImageView
					newImage.SetImageBitmap(((BitmapDrawable) image.Drawable).Bitmap);
					newImage.SetScaleType(image.GetScaleType());
					newImage.LayoutParameters = layoutI;
					newImage.SetBackgroundColor(((ColorDrawable) image.Background).Color);
					newImage.SetZ(image.GetZ());
					newImage.Click += SelectI;
					newImage.FocusChange += Focus;

					carta.AddView(newImage);
				} else {
					EditText label = (EditText) selected;
					EditText newLabel = new EditText(carta.Context);


					//Cria as configurações de tamanho e posicionamento do Texto
					RelativeLayout.LayoutParams layoutT = new RelativeLayout.LayoutParams(label.LayoutParameters.Width, label.LayoutParameters.Height);
					layoutT.LeftMargin = ((RelativeLayout.LayoutParams)label.LayoutParameters).LeftMargin + 5;
					layoutT.TopMargin = ((RelativeLayout.LayoutParams)label.LayoutParameters).TopMargin + 5;

					//Copia as configurações para o novo EditText
					newLabel.SetTextSize(Android.Util.ComplexUnitType.Dip, PxToDip((int)label.TextSize));
					newLabel.SetText(label.Text, TextView.BufferType.Editable);
					newLabel.Typeface = label.Typeface;
					newLabel.Gravity = label.Gravity;
					newLabel.LayoutParameters = layoutT;
					newLabel.SetTextColor(new Color(label.CurrentTextColor));
					newLabel.SetBackgroundColor(((ColorDrawable) label.Background).Color);
					newLabel.SetZ(label.GetZ());
					newLabel.Click += SelectT;
					newLabel.FocusChange += Focus;

					carta.AddView(newLabel);
					SelectT(newLabel, null);

					Toast.MakeText(this, "Duplicado.", ToastLength.Short).Show();
				}
			}
		}

		public void Focus (object sender, View.FocusChangeEventArgs e) {
			if (e.HasFocus) {
				SelectT(sender);
			}
		}

		public void TitleFocus (object sender, View.FocusChangeEventArgs e) {
			if (e.HasFocus) {
				Unselect();
			}
		}

		public void ChangeFontSize (object sender, AfterTextChangedEventArgs e) {
			if (selected != null && !isImage) {
				EditText label = (EditText)selected;
				try {
					label.TextSize = int.Parse(FindViewById<AutoCompleteTextView>(Resource.Id.fontSize).Text);
				} catch {
					Toast.MakeText(this, "Valor inválido.", ToastLength.Short).Show();
				}
			}
		}

		public void Negrito (object sender, EventArgs e) {
			if (selected != null && !isImage) {
				EditText label = (EditText)selected;
				Button clicked = (Button)sender;

				switch (label.Typeface.Style) {
					case TypefaceStyle.Bold:
						label.SetTypeface(Typeface.Default, TypefaceStyle.Normal);
						clicked.SetBackgroundColor(Color.DimGray);
						break;
					case TypefaceStyle.BoldItalic:
						label.SetTypeface(label.Typeface, TypefaceStyle.Italic);
						clicked.SetBackgroundColor(Color.DimGray);
						break;
					case TypefaceStyle.Italic:
						label.SetTypeface(label.Typeface, TypefaceStyle.BoldItalic);
						clicked.SetBackgroundColor(new Color(68, 119, 255));
						break;
					case TypefaceStyle.Normal:
						label.SetTypeface(label.Typeface, TypefaceStyle.Bold);
						clicked.SetBackgroundColor(new Color(68, 119, 255));
						break;
				}
			}
		}

		public void Italico (object sender, EventArgs e) {
			if (selected != null && !isImage) {
				EditText label = (EditText)selected;
				Button clicked = (Button)sender;

				switch (label.Typeface.Style) {
					case TypefaceStyle.Bold:
						label.SetTypeface(label.Typeface, TypefaceStyle.BoldItalic);
						clicked.SetBackgroundColor(new Color(68, 119, 255));
						break;
					case TypefaceStyle.BoldItalic:
						label.SetTypeface(label.Typeface, TypefaceStyle.Bold);
						clicked.SetBackgroundColor(Color.DimGray);
						break;
					case TypefaceStyle.Italic:
						label.SetTypeface(Typeface.Default, TypefaceStyle.Normal);
						clicked.SetBackgroundColor(Color.DimGray);
						break;
					case TypefaceStyle.Normal:
						label.SetTypeface(label.Typeface, TypefaceStyle.Italic);
						clicked.SetBackgroundColor(new Color(68, 119, 255));
						break;
				}
			}
		}

		public void ChangeAlign (object sender, AdapterView.ItemSelectedEventArgs e) {
			if (selected != null && !isImage) {
				EditText label = (EditText)selected;

				switch (aligns[e.Position]) {
					case "Superior-Esquerdo":
						label.Gravity = GravityFlags.Left;
						break;
					case "Superior-Centro":
						label.Gravity = GravityFlags.CenterHorizontal;
						break;
					case "Superior-Direito":
						label.Gravity = GravityFlags.Right;
						break;
					case "Centro-Esquerdo":
						label.Gravity = GravityFlags.CenterVertical;
						break;
					case "Centro-Centro":
						label.Gravity = GravityFlags.Center;
						break;
					case "Centro-Direito":
						label.Gravity = GravityFlags.CenterVertical | GravityFlags.Right;
						break;
					case "Inferior-Esquerdo":
						label.Gravity = GravityFlags.Bottom;
						break;
					case "Inferior-Centro":
						label.Gravity = GravityFlags.Bottom | GravityFlags.CenterHorizontal;
						break;
					case "Inferior-Direito":
						label.Gravity = GravityFlags.Bottom | GravityFlags.Right;
						break;
				}
			}
		}

		public void ChangeScale (object sender, AdapterView.ItemSelectedEventArgs e) {
			if (selected != null && isImage) {
				ImageView image = (ImageView) selected;

				switch (scales[e.Position]) {
					case "Dentro-Esquerda":
						image.SetScaleType(ImageView.ScaleType.FitStart);
						break;
					case "Dentro-Centro":
						image.SetScaleType(ImageView.ScaleType.CenterInside);
						break;
					case "Dentro-Direita":
						image.SetScaleType(ImageView.ScaleType.FitEnd);
						break;
					case "Ajustar":
						image.SetScaleType(ImageView.ScaleType.FitXy);
						break;
					case "Centro":
						image.SetScaleType(ImageView.ScaleType.Center);
						break;
					case "Cortar-Centro":
						image.SetScaleType(ImageView.ScaleType.CenterCrop);
						break;
					case "Não Ajustar":
						image.SetScaleType(ImageView.ScaleType.Matrix);
						break;
				}
			}
		}

		public void CancelColor (object sender, EventArgs e) {
			//Voltar ao original
			if (request == 1)
				((EditText) selected).SetTextColor(originalColor);
			else if (request == 2 || request == 3)
				selected.SetBackgroundColor(originalColor);
			else if (request == 4)
				carta.SetBackgroundColor(originalColor);

			request = 0;
			InvalidateOptionsMenu();
		}

		public void ConfirmColor (object sender, EventArgs e) {
			request = 0;
			InvalidateOptionsMenu();
		}

		public int GetAlign (GravityFlags gravity) {
			switch (gravity) {
				case GravityFlags.Left:
					return 0;
				case GravityFlags.CenterHorizontal:
					return 1;
				case GravityFlags.Right:
					return 2;
				case GravityFlags.CenterVertical:
					return 3;
				case GravityFlags.Center:
					return 4;
				case GravityFlags.CenterVertical | GravityFlags.Right:
					return 5;
				case GravityFlags.Bottom:
					return 6;
				case GravityFlags.Bottom | GravityFlags.CenterHorizontal:
					return 7;
				case GravityFlags.Bottom | GravityFlags.Right:
					return 8;
				default:
					return -1;
			}
		}

		public int GetScale (ImageView.ScaleType scaleType) {
			if (scaleType == ImageView.ScaleType.FitStart) {
				return 0;
			} else if (scaleType == ImageView.ScaleType.CenterInside) {
				return 1;
			} else if (scaleType == ImageView.ScaleType.FitEnd) {
				return 2;
			} else if (scaleType == ImageView.ScaleType.FitXy) {
				return 3;
			} else if (scaleType == ImageView.ScaleType.Center) {
				return 4;
			} else if (scaleType == ImageView.ScaleType.CenterCrop) {
				return 5;
			} else if (scaleType == ImageView.ScaleType.Matrix) {
				return 6;
			} else {
				return 0;
			}
		}

		public int DipToPx (int dp) {
			return (int) Android.Util.TypedValue.ApplyDimension(Android.Util.ComplexUnitType.Dip, dp, Resources.DisplayMetrics);
		}

		public int PxToDip (int px) {
			return (int) (px / Resources.DisplayMetrics.Density);
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

		public void ColorView (int current) {
			if (relativeLayout == null)
				relativeLayout = FindViewById<RelativeLayout>(Resource.Id.relativeLayout1);

			if (selected != null) {
				selected.Id = 911;
				relativeLayout.RemoveView(FindViewById<GridLayout>(Resource.Id.gridLayout1)); //Unselect on draw

				selected = FindViewById(911);
				selected.Id = 0;
			} else {
				relativeLayout.RemoveView(FindViewById<GridLayout>(Resource.Id.gridLayout1));
			}

			try {
				LayoutInflater.Inflate(Resource.Layout.ColorTools, relativeLayout);

				//Configurar tela
				FindViewById<View>(Resource.Id.choosen).SetBackgroundColor(new Color(current));

				//Red
				SeekBar rSlider = FindViewById<SeekBar>(Resource.Id.rSlider);
				EditText red = FindViewById<EditText>(Resource.Id.red);
				rSlider.ProgressChanged += delegate { ColorChanged(rSlider, "RS"); };
				red.AfterTextChanged += delegate { ColorChanged(red, "RB"); };
				rSlider.Progress = Color.GetRedComponent(current);
				red.Text = rSlider.Progress.ToString();

				//Green
				SeekBar gSlider = FindViewById<SeekBar>(Resource.Id.gSlider);
				EditText green = FindViewById<EditText>(Resource.Id.green);
				gSlider.ProgressChanged += delegate { ColorChanged(gSlider, "GS"); };
				green.AfterTextChanged += delegate { ColorChanged(green, "GB"); };
				gSlider.Progress = Color.GetGreenComponent(current);
				green.Text = gSlider.Progress.ToString();

				//Blue
				SeekBar bSlider = FindViewById<SeekBar>(Resource.Id.bSlider);
				EditText blue = FindViewById<EditText>(Resource.Id.blue);
				bSlider.ProgressChanged += delegate { ColorChanged(bSlider, "BS"); };
				blue.AfterTextChanged += delegate { ColorChanged(blue, "BB"); };
				bSlider.Progress = Color.GetBlueComponent(current);
				blue.Text = bSlider.Progress.ToString();

				//Adicionar funcionalidades
				//Color buttons
				FindViewById<View>(Resource.Id.color1V).Click += Paleta;
				FindViewById<View>(Resource.Id.color2V).Click += Paleta;
				FindViewById<View>(Resource.Id.color3V).Click += Paleta;
				FindViewById<View>(Resource.Id.color4V).Click += Paleta;
				FindViewById<View>(Resource.Id.color5V).Click += Paleta;
				FindViewById<View>(Resource.Id.color6V).Click += Paleta;
				FindViewById<View>(Resource.Id.color7V).Click += Paleta;
				FindViewById<View>(Resource.Id.color8V).Click += Paleta;
				FindViewById<View>(Resource.Id.color9V).Click += Paleta;
				FindViewById<View>(Resource.Id.color10V).Click += Paleta;
				FindViewById<View>(Resource.Id.color11V).Click += Paleta;
				FindViewById<View>(Resource.Id.color12V).Click += Paleta;

				//Dialog
				FindViewById<Button>(Resource.Id.cancelColor).Click += CancelColor;
				FindViewById<Button>(Resource.Id.confirmColor).Click += ConfirmColor;
			} catch (Exception err) {
				Toast.MakeText(this, "2!: " + err.Message, ToastLength.Long).Show();
			}
		}

		public void PositionView () {
			if (relativeLayout == null)
				relativeLayout = FindViewById<RelativeLayout>(Resource.Id.relativeLayout1);

			shown = false;
			shown2 = false;

			selected.Id = 911;
			relativeLayout.RemoveView(FindViewById<GridLayout>(Resource.Id.gridLayout1)); //Unselect on draw

			selected = FindViewById(911);
			selected.Id = 0;
			try {
				LayoutInflater.Inflate(Resource.Layout.PositionTools, relativeLayout);

				RelativeLayout.LayoutParams layout = (RelativeLayout.LayoutParams) selected.LayoutParameters;
				originalLayout = new RelativeLayout.LayoutParams(layout);

				//Configurar tela
				EditText x = FindViewById<EditText>(Resource.Id.x);
				EditText y = FindViewById<EditText>(Resource.Id.y);
				EditText z = FindViewById<EditText>(Resource.Id.z);

				x.Text = PxToDip(layout.LeftMargin).ToString();
				y.Text = PxToDip(layout.TopMargin).ToString();
				z.Text = selected.GetZ().ToString();

				x.AfterTextChanged += delegate { Positioning('X', x.Text); };
				y.AfterTextChanged += delegate { Positioning('Y', y.Text); };
				z.AfterTextChanged += delegate { Positioning('Z', z.Text); };

				//Adicionar funcionalidades
				FindViewById<Button>(Resource.Id.plusX).Click += delegate {
					int old;
					int.TryParse(x.Text, out old);

					x.Text = (old + 5).ToString();
				};
				FindViewById<Button>(Resource.Id.lessX).Click += delegate {
					int old;
					int.TryParse(x.Text, out old);

					if (old > 4)
						x.Text = (old - 5).ToString();
					else
						x.Text = "0";
				};

				FindViewById<Button>(Resource.Id.plusY).Click += delegate {
					int old;
					int.TryParse(y.Text, out old);

					y.Text = (old + 5).ToString();
				};
				FindViewById<Button>(Resource.Id.lessY).Click += delegate {
					int old;
					int.TryParse(y.Text, out old);

					if (old > 4)
						y.Text = (old - 5).ToString();
					else
						y.Text = "0";
				};

				FindViewById<Button>(Resource.Id.voltar).Click += delegate {
					selected.LayoutParameters = originalLayout;

					InvalidateOptionsMenu();
				};

				FindViewById<Button>(Resource.Id.confirmPos).Click += delegate {
					InvalidateOptionsMenu();
				};
			} catch (Exception err) {
				Toast.MakeText(this, "6!: " + err.Message, ToastLength.Long).Show();
			}
		}

		public void ResizeView () {
			if (relativeLayout == null)
				relativeLayout = FindViewById<RelativeLayout>(Resource.Id.relativeLayout1);

			shown = false;
			shown2 = false;

			selected.Id = 911;
			relativeLayout.RemoveView(FindViewById<GridLayout>(Resource.Id.gridLayout1)); //Unselect on draw

			selected = FindViewById(911);
			selected.Id = 0;
			try {
				LayoutInflater.Inflate(Resource.Layout.SizeTools, relativeLayout);

				RelativeLayout.LayoutParams layout = (RelativeLayout.LayoutParams) selected.LayoutParameters;
				originalLayout = new RelativeLayout.LayoutParams(layout);

				//Configurar tela
				EditText w = FindViewById<EditText>(Resource.Id.width);
				EditText h = FindViewById<EditText>(Resource.Id.height);

				if (isImage) {
					w.Text = PxToDip(layout.Width).ToString();
					h.Text = PxToDip(layout.Height).ToString();
				} else {
					w.Text = PxToDip(selected.MeasuredWidth).ToString();
					h.Text = PxToDip(selected.MeasuredHeight).ToString();
				}

				w.AfterTextChanged += delegate { Resizing('W', w.Text); };
				h.AfterTextChanged += delegate { Resizing('H', h.Text); };

				//Adicionar funcionalidades
				FindViewById<Button>(Resource.Id.plusW).Click += delegate {
					int old;
					int.TryParse(w.Text, out old);

					w.Text = (old + 5).ToString();
				};
				FindViewById<Button>(Resource.Id.lessW).Click += delegate {
					int old;
					int.TryParse(w.Text, out old);

					if (old > 4)
						w.Text = (old - 5).ToString();
					else
						w.Text = "0";
				};

				FindViewById<Button>(Resource.Id.plusH).Click += delegate {
					int old;
					int.TryParse(h.Text, out old);

					h.Text = (old + 5).ToString();
				};
				FindViewById<Button>(Resource.Id.lessH).Click += delegate {
					int old;
					int.TryParse(h.Text, out old);

					if (old > 4)
						h.Text = (old - 5).ToString();
					else
						h.Text = "0";
				};

				FindViewById<Button>(Resource.Id.voltar).Click += delegate {
					selected.LayoutParameters = originalLayout;

					InvalidateOptionsMenu();
				};

				FindViewById<Button>(Resource.Id.confirmSize).Click += delegate { InvalidateOptionsMenu(); };
			} catch (Exception err) {
				Toast.MakeText(this, "7!: " + err.Message, ToastLength.Long).Show();
			}
		}

		public void Paleta (object sender, EventArgs e) {
			Color color = ((ColorDrawable) ((View) sender).Background).Color;

			FindViewById<SeekBar>(Resource.Id.rSlider).Progress = Color.GetRedComponent(color);
			FindViewById<SeekBar>(Resource.Id.gSlider).Progress = Color.GetGreenComponent(color);
			FindViewById<SeekBar>(Resource.Id.bSlider).Progress = Color.GetBlueComponent(color);
		}

		public void ColorChanged (View control, string type) {
			int val = -1;
			string newValue;
			EditText ctr;

			switch (type) {
				case "RS":
					newValue = ((SeekBar) control).Progress.ToString();
					ctr = FindViewById<EditText>(Resource.Id.red);

					if (ctr.Text != newValue)
						ctr.Text = newValue;
					break;
				case "GS":
					newValue = ((SeekBar) control).Progress.ToString();
					ctr = FindViewById<EditText>(Resource.Id.green);

					if (ctr.Text != newValue)
						ctr.Text = newValue;
					break;
				case "BS":
					newValue = ((SeekBar) control).Progress.ToString();
					ctr = FindViewById<EditText>(Resource.Id.blue);

					if (ctr.Text != newValue)
						ctr.Text = newValue;
					break;
				case "RB":
					ctr = (EditText) control;
					int.TryParse(ctr.Text, out val);

					if (val >= 0 && val <= 255)
						FindViewById<SeekBar>(Resource.Id.rSlider).Progress = val;
					else
						ctr.Text = (val > 255) ? "255" : "0";
					break;
				case "GB":
					ctr = (EditText) control;
					int.TryParse(ctr.Text, out val);

					if (val >= 0 && val <= 255)
						FindViewById<SeekBar>(Resource.Id.gSlider).Progress = val;
					else
						ctr.Text = (val > 255) ? "255" : "0";
					break;
				case "BB":
					ctr = (EditText) control;
					int.TryParse(ctr.Text, out val);

					if (val >= 0 && val <= 255)
						FindViewById<SeekBar>(Resource.Id.bSlider).Progress = val;
					else
						ctr.Text = (val > 255) ? "255" : "0";
					break;
			}

			Color color = new Color(
				FindViewById<SeekBar>(Resource.Id.rSlider).Progress,
				FindViewById<SeekBar>(Resource.Id.gSlider).Progress,
				FindViewById<SeekBar>(Resource.Id.bSlider).Progress
			);
			
			FindViewById<View>(Resource.Id.choosen).SetBackgroundColor(color);
			if (request == 1)
				((EditText) selected).SetTextColor(color);
			else if (request == 2 || request == 3)
				selected.SetBackgroundColor(color);
			else if (request == 4)
				carta.SetBackgroundColor(color);
		}

		public void Positioning (char axis, string value) {
			switch (axis) {
				case 'X':
					try {
						int left = int.Parse(value);
						RelativeLayout.LayoutParams layout = new RelativeLayout.LayoutParams((RelativeLayout.LayoutParams) selected.LayoutParameters);

						if (DipToPx(350) - layout.Width < DipToPx(left)) {
							left = DipToPx(350) - layout.Width;
							if (!shown) {
								Toast.MakeText(this, "Valor máximo: " + PxToDip(left), ToastLength.Short).Show();
								shown = true;
							}
						} else if (left < 0) {
							left = 0;
							Toast.MakeText(this, "Valor mínimo: 0", ToastLength.Short).Show();
						} else {
							left = DipToPx(left);
							shown = false;
						}

						layout.LeftMargin = left;
						selected.LayoutParameters = layout;
					} catch {
						Toast.MakeText(this, "Valor inválido.", ToastLength.Short).Show();
					}

					break;
				case 'Y':
					try {
						int top = int.Parse(value);
						RelativeLayout.LayoutParams layout = new RelativeLayout.LayoutParams((RelativeLayout.LayoutParams) selected.LayoutParameters);

						if (DipToPx(350) - layout.Height < DipToPx(top)) {
							top = DipToPx(350) - layout.Height;
							if (!shown2) {
								Toast.MakeText(this, "Valor máximo: " + PxToDip(top), ToastLength.Short).Show();
								shown2 = true;
							}
						} else if (top < 0) {
							top = 0;
							Toast.MakeText(this, "Valor mínimo: 0", ToastLength.Short).Show();
						} else {
							top = DipToPx(top);
							shown2 = false;
						}

						layout.TopMargin = top;
						selected.LayoutParameters = layout;
					} catch {
						Toast.MakeText(this, "Valor inválido.", ToastLength.Short).Show();
					}

					break;
				case 'Z':
					try {
						float layer = int.Parse(value);
						selected.SetZ(layer);
					} catch {
						Toast.MakeText(this, "Valor inválido.", ToastLength.Short).Show();
					}

					break;
			}
		}

		public void Resizing (char axis, string value) {
			switch (axis) {
				case 'W':
					try {
						int width = int.Parse(value);
						RelativeLayout.LayoutParams layout = new RelativeLayout.LayoutParams((RelativeLayout.LayoutParams) selected.LayoutParameters);

						if (DipToPx(350) - layout.LeftMargin < DipToPx(width)) {
							width = DipToPx(350) - layout.LeftMargin;
							if (!shown) {
								Toast.MakeText(this, "Valor máximo: " + PxToDip(width), ToastLength.Short).Show();
								shown = true;
							}
						} else if (width < 0) {
							width = 0;
							Toast.MakeText(this, "Valor mínimo: 0", ToastLength.Short).Show();
						} else {
							width = DipToPx(width);
							shown = false;
						}

						layout.Width = width;
						selected.LayoutParameters = layout;
					} catch {
						Toast.MakeText(this, "Valor inválido.", ToastLength.Short).Show();
					}

					break;
				case 'H':
					try {
						int height = int.Parse(value);
						RelativeLayout.LayoutParams layout = new RelativeLayout.LayoutParams((RelativeLayout.LayoutParams) selected.LayoutParameters);

						if (DipToPx(350) - layout.TopMargin < DipToPx(height)) {
							height = DipToPx(350) - layout.TopMargin;
							if (!shown2) {
								Toast.MakeText(this, "Valor máximo: " + PxToDip(height), ToastLength.Short).Show();
								shown2 = true;
							}
						} else if (height < 0) {
							height = 0;
							Toast.MakeText(this, "Valor mínimo: 0", ToastLength.Short).Show();
						} else {
							height = DipToPx(height);
							shown2 = false;
						}

						layout.Height = height;
						selected.LayoutParameters = layout;
					} catch {
						Toast.MakeText(this, "Valor inválido.", ToastLength.Short).Show();
					}

					break;
			}
		}

		protected override void OnActivityResult (int requestCode, [GeneratedEnum] Result resultCode, Intent data) {
			if (resultCode == Result.Ok) {
				if (data.Data != null && requestCode == 1) {
					AddImage(BitmapFactory.DecodeStream(ContentResolver.OpenInputStream(data.Data)));
				} else if (data.Data != null && requestCode == 2 && isImage) {
					((ImageView) selected).SetImageBitmap(BitmapFactory.DecodeStream(ContentResolver.OpenInputStream(data.Data)));
				}
			}

			base.OnActivityResult(requestCode, resultCode, data);
		}

		public void OnClick (IDialogInterface dialog, int which) {
			request = 4;

			try {
				originalColor = ((ColorDrawable) carta.Background).Color;
			} catch {
				carta.SetBackgroundColor(Color.White);
				originalColor = Color.White;
			}

			ColorView(originalColor);
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
				FindViewById<Button>(Resource.Id.EditarVerso).Click += Turn;

				FindViewById<Button>(Resource.Id.AddImg).Click += delegate { AddImg(false); };
				FindViewById<Button>(Resource.Id.BackgroundColor).Click += delegate {
					try {
						originalColor = ((ColorDrawable) carta.Background).Color;

						request = 4;
						ColorView(originalColor);
					} catch {
						new AlertDialog.Builder(this)
							.SetTitle(Resource.String.ConfirmBackgroundTitle)
							.SetMessage(Resource.String.ConfirmBackground)
							.SetPositiveButton(Resource.String.Yes, this)
							.SetNegativeButton(Resource.String.No, (IDialogInterfaceOnClickListener) null)
							.Show();
					}
				};
			} else if (isImage) {
				MenuInflater.Inflate(Resource.Menu.ImageSelected, menu);

				if (relativeLayout == null)
					relativeLayout = FindViewById<RelativeLayout>(Resource.Id.relativeLayout1);
				
				selected.Id = 911;
				relativeLayout.RemoveView(FindViewById<GridLayout>(Resource.Id.gridLayout1)); //Unselect on draw

				selected = FindViewById(911);
				selected.Id = 0;
				isImage = true;

				try {
					LayoutInflater.Inflate(Resource.Layout.ImageTools, relativeLayout);

					scales = new string[] { "Dentro-Esquerda", "Dentro-Centro", "Dentro-Direita", "Ajustar", "Centro", "Cortar-Centro", "Não Ajustar" };

					Spinner scale = FindViewById<Spinner>(Resource.Id.scale);
					scale.Adapter = new ArrayAdapter(this, Android.Resource.Layout.SimpleDropDownItem1Line, scales);
					scale.SetSelection(GetScale(((ImageView) selected).GetScaleType()));
					scale.ItemSelected += ChangeScale;

					FindViewById<Button>(Resource.Id.ImageBackground).Click += delegate {
						request = 3;
						originalColor = ((ColorDrawable) selected.Background).Color;
						ColorView(originalColor);
					};

					FindViewById<Button>(Resource.Id.changeImg).Click += delegate { AddImg(true); };
					FindViewById<Button>(Resource.Id.duplicar).Click += Duplicar;
				} catch (Exception err) {
					Toast.MakeText(this, "5!: " + err.Message, ToastLength.Long).Show();
				}
			} else {
				MenuInflater.Inflate(Resource.Menu.LabelSelected, menu);

				if (relativeLayout == null)
					relativeLayout = FindViewById<RelativeLayout>(Resource.Id.relativeLayout1);

				selected.Id = 911;
				relativeLayout.RemoveView(FindViewById<GridLayout>(Resource.Id.gridLayout1)); //Unselect on draw

				selected = FindViewById(911);
				selected.Id = 0;
				isImage = false;

				try {
					LayoutInflater.Inflate(Resource.Layout.LabelTools, relativeLayout);

					AutoCompleteTextView fontS = FindViewById<AutoCompleteTextView>(Resource.Id.fontSize);
					fontS.Adapter = new ArrayAdapter(this, Android.Resource.Layout.SimpleDropDownItem1Line, new string[] { "8", "9", "10", "11", "12", "14", "16", "18", "20", "22", "24", "26", "28", "32", "36", "42", "48", "72" });
					fontS.Text = PxToDip((int) ((EditText)selected).TextSize).ToString();
					fontS.AfterTextChanged += ChangeFontSize;

					aligns = new string[] { "Superior-Esquerdo", "Superior-Centro", "Superior-Direito", "Centro-Esquerdo", "Centro-Centro", "Centro-Direito", "Inferior-Esquerdo", "Inferior-Centro", "Inferior-Direito" };

					Spinner align = FindViewById<Spinner>(Resource.Id.align);
					align.Adapter = new ArrayAdapter(this, Android.Resource.Layout.SimpleDropDownItem1Line, aligns);
					align.SetSelection(GetAlign(((EditText)selected).Gravity));
					align.ItemSelected += ChangeAlign;

					TypefaceStyle style = ((EditText) selected).Typeface.Style;

					Button negrito = FindViewById<Button>(Resource.Id.negrito);
					negrito.Click += Negrito;
					if (style == TypefaceStyle.Bold || style == TypefaceStyle.BoldItalic)
						negrito.SetBackgroundColor(new Color(68, 119, 255));
					else
						negrito.SetBackgroundColor(Color.DimGray);

					Button italico = FindViewById<Button>(Resource.Id.itálico);
					italico.Click += Italico;
					if (style == TypefaceStyle.Italic || style == TypefaceStyle.BoldItalic)
						italico.SetBackgroundColor(new Color(68, 119, 255));
					else
						italico.SetBackgroundColor(Color.DimGray);

					FindViewById<Button>(Resource.Id.duplicar).Click += Duplicar;
				} catch (Exception err) {
					Toast.MakeText(this, "!: " + err.Message, ToastLength.Long).Show();
				}
			}

			return base.OnCreateOptionsMenu(menu);
		}

		public override bool OnOptionsItemSelected (IMenuItem item) {
			switch (item.ItemId) {
				case Android.Resource.Id.Home:
					Finish();
					break;
				case Resource.Id.TextColor:
					request = 1;
					originalColor = new Color(((EditText) selected).CurrentTextColor);
					ColorView(originalColor);
					break;
				case Resource.Id.TextBackground:
					request = 2;
					originalColor = ((ColorDrawable) selected.Background).Color;
					ColorView(originalColor);
					break;
				case Resource.Id.BackgroundTransparent:
					selected.SetBackgroundColor(Color.Transparent);
					break;
				case Resource.Id.unselect:
					Unselect();
					break;
				case Resource.Id.RemoveView:
					carta.RemoveView(selected);
					Unselect();
					break;
				case Resource.Id.PositionView:
					PositionView();
					break;
				case Resource.Id.ResizeView:
					ResizeView();
					break;
				case Resource.Id.ResetImage:
					((ImageView) selected).SetImageBitmap(null);
					break;
				case Resource.Id.UseAsBackground:
					//Set
					BitmapDrawable newBackground = (BitmapDrawable) ((ImageView) selected).Drawable;
					newBackground.SetTileModeXY(Shader.TileMode.Repeat, Shader.TileMode.Repeat);
					carta.Background = newBackground;

					//Clean
					carta.RemoveView(selected);
					Unselect();
					break;
				case Resource.Id.ResetBackground:
					carta.SetBackgroundColor(Color.White);
					break;
				case Resource.Id.HorizontalCenter:
					HorizontalCenter();
					break;
				case Resource.Id.VerticalCenter:
					VerticalCenter();
					break;
				default:
					return base.OnOptionsItemSelected(item);
			}

			return true;
		}
	}
}