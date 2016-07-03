using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

using Android.App;
using Android.Content;
using Android.Database;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Provider;
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

	[Activity(Label = "@string/EditorCartas", ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait, Theme = "@style/CustomActionBarTheme")]
	public class EditorCarta : Activity, IDialogInterfaceOnClickListener {
		private int cardID;
		private string side;
		private string xmlDir;
		private string appPath;
		private string baralhoFileName;
		private string[] aligns;
		private string[] scales;
		private View selected;
		private Baralhos baralho;
		private EditText title;
		private RelativeLayout carta;
		private RelativeLayout relativeLayout;

		private int request;
		private int requestAlert;
		private int currentDialog;
		private bool shown;
		private bool shown2;
		private Color originalColor;
		private List<string> toDelete;
		private List<string> notSavedImages;
		private List<Bitmap> images;
		private RelativeLayout.LayoutParams originalLayout;

		protected override void OnCreate (Bundle savedInstanceState) {
			base.OnCreate(savedInstanceState);

			// Create your application here
			SetContentView(Resource.Layout.EditorCarta);

			ActionBar.SetDisplayHomeAsUpEnabled(true);

			images = new List<Bitmap>();
			toDelete = new List<string>();
			notSavedImages = new List<string>();

			baralho = SGBD.AcessarBaralho(Intent.GetIntExtra("Baralho_ID", -1));
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

			Read(Intent.GetBooleanExtra("Copying", false) ? ((side == "Frente") ? "Verso" : "Frente") : side, Intent.GetBooleanExtra("Copying", false));
		}

		public void Read (string side, bool copy = false) {
			int length, toAdd = 0;
			string imgSrc, path, copied, extension;
			string fileDir = System.IO.Path.Combine(appPath, baralhoFileName.Replace(".lrv", " Resources"), "img.length");
			Dictionary<string, string> srcCopied = new Dictionary<string, string>();

			if (File.Exists(fileDir)) {
				StreamReader sr = new StreamReader(fileDir);
				int.TryParse(sr.ReadToEnd(), out length);
				sr.Close();
			} else {
				length = 0;
				StreamWriter sw = File.CreateText(fileDir);
				sw.Write(0);
				sw.Close();
			}

			using (XmlReader xmlR = XmlReader.Create(xmlDir)) {
				while (xmlR.Read()) {
					if (xmlR.IsStartElement("Carta")) {
						if (xmlR["id"] == cardID.ToString()) {
							string nome = xmlR["name"];
							title.Text = nome;

							while (!xmlR.IsStartElement(side))
								xmlR.Read();

							if (xmlR.IsStartElement(side)) {
								string background = xmlR["background"];
								if (background.Length > 0)
									carta.SetBackgroundColor(new Color(int.Parse(background)));
								else
									carta.SetBackgroundColor(Color.White);

								string cardSrc = xmlR["src"];

								if (cardSrc.Length > 0) {
									if (copy) {
										imgSrc = "";
										foreach (KeyValuePair<string, string> src in srcCopied) {
											if (cardSrc == src.Key) {
												imgSrc = src.Value;
											}
										}

										if (imgSrc == "") {
											try {
												extension = cardSrc.Substring(cardSrc.LastIndexOf("."));
											} catch {
												extension = ".png";
											}

											imgSrc = "img-" + cardID + "." + (length + toAdd) + extension;

											path = System.IO.Path.Combine(appPath, baralhoFileName.Replace(".lrv", " Resources"), cardSrc);
											copied = System.IO.Path.Combine(appPath, baralhoFileName.Replace(".lrv", " Resources"), imgSrc);

											File.Copy(path, copied);

											srcCopied.Add(cardSrc, imgSrc);
											toAdd++;
										}

										cardSrc = imgSrc;
									}

									Bitmap image;
									try {
										string file = System.IO.Path.Combine(appPath, baralhoFileName.Replace(".lrv", " Resources"), cardSrc);

										BitmapFactory.Options options = new BitmapFactory.Options();
										options.InJustDecodeBounds = true;

										BitmapFactory.DecodeFile(file, options);
										options.InSampleSize = CalculateInSample(options, DipToPx(310), DipToPx(310));
										options.InJustDecodeBounds = false;

										image = BitmapFactory.DecodeFile(file, options);
										images.Add(image);
									} catch {
										image = null;
									}

									if (image != null) {
										carta.Background = new BitmapDrawable(image);
										carta.SetTag(Resource.Id.imgSrc, cardSrc);
									} else {
										carta.SetTag(Resource.Id.imgSrc, "");
									}
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
													string text = "";
													int fontS = 12;
													int gravity = -1;

													int.TryParse(xmlR["fontSize"], out fontS);
													int.TryParse(xmlR["gravity"], out gravity);

													string style = xmlR["style"];
													Rect rectT = new Rect(xmlR["rect"]);
													Color color = new Color(int.Parse(xmlR["color"]));
													Color backgroundT = new Color(int.Parse(xmlR["background"]));
													int zIndex = int.Parse(xmlR["z"]);

													if (xmlR.Read())
														text = xmlR.Value.Trim();

													//Cria as configurações de tamanho e posicionamento do Texto
													RelativeLayout.LayoutParams layoutT = new RelativeLayout.LayoutParams(DipToPx(rectT.width), DipToPx(rectT.height));
													layoutT.LeftMargin = DipToPx(rectT.x);
													layoutT.TopMargin = DipToPx(rectT.y);

													//Aplica as configurações no novo EditText
													newLabel.SetTextSize(Android.Util.ComplexUnitType.Dip, fontS);
													newLabel.SetText(text, TextView.BufferType.Editable);
													newLabel.SetTypeface(Typeface.SansSerif, GetTypeFace(style));
													newLabel.Gravity = GetAlign(gravity);
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
													//Lê as configurações dos atributos da Imagem no XML
													string srcI = xmlR["src"];
													Bitmap image;

													if (srcI.Length > 0) {
														if (copy) {
															imgSrc = "";
															foreach (KeyValuePair<string, string> src in srcCopied) {
																if (srcI == src.Key) {
																	imgSrc = src.Value;
																}
															}

															if (imgSrc == "") {
																try {
																	extension = srcI.Substring(srcI.LastIndexOf("."));
																} catch {
																	extension = ".png";
																}

																imgSrc = "img-" + cardID + "." + (length + toAdd) + extension;

																path = System.IO.Path.Combine(appPath, baralhoFileName.Replace(".lrv", " Resources"), srcI);
																copied = System.IO.Path.Combine(appPath, baralhoFileName.Replace(".lrv", " Resources"), imgSrc);

																File.Copy(path, copied);

																srcCopied.Add(srcI, imgSrc);
																toAdd++;
															}

															srcI = imgSrc;
														}

														try {
															string file = System.IO.Path.Combine(appPath, baralhoFileName.Replace(".lrv", " Resources"), srcI);

															BitmapFactory.Options options = new BitmapFactory.Options();
															options.InJustDecodeBounds = true;

															BitmapFactory.DecodeFile(file, options);
															options.InSampleSize = CalculateInSample(options, DipToPx(310), DipToPx(310));
															options.InJustDecodeBounds = false;

															image = BitmapFactory.DecodeFile(file, options);
															images.Add(image);
														} catch {
															Toast.MakeText(this, srcI + " não encontrado.", ToastLength.Long).Show();
															image = null;
															continue;
														}
													} else {
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
													ImageView newImage = new ImageView(carta.Context);

													if (image != null) {
														newImage.SetImageBitmap(image);
														newImage.SetTag(Resource.Id.imgSrc, srcI);
														if (copy) 
															notSavedImages.Add(srcI);
													} else {
														newImage.SetImageResource(Android.Resource.Color.Transparent);
														newImage.SetTag(Resource.Id.imgSrc, "");
													}
													newImage.SetScaleType(ImageView.ScaleType.ValueOf(scale));
													newImage.LayoutParameters = layoutI;
													newImage.SetBackgroundColor(backgroundI);
													newImage.SetZ(zIndex);
													newImage.Click += SelectI;

													carta.AddView(newImage);
												} catch (Exception err) {
													Toast.MakeText(this, "Erro ao criar uma imagem: " + err.Message, ToastLength.Long).Show();
												}
												break;
											default:
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

			if (toAdd > 0) {
				StreamWriter sw = new StreamWriter(fileDir);
				sw.Write(length + toAdd);
				sw.Close();
			}
		}

		public void SaveCard (int nextAction = -1) {
			try {
				string res = System.IO.Path.Combine(appPath, baralhoFileName.Replace(".lrv", " Resources"));

				XDocument cartasXML = XDocument.Load(xmlDir);
				XElement xCarta = (from card in cartasXML.Element("Cartas").Elements("Carta")
								  where card.Attribute("id").Value == cardID.ToString()
								  select card).ToArray()[0];

				xCarta.SetAttributeValue("name", title.Text);
				XElement editing = xCarta.Element(side);

				var sources = from images in editing.Elements("Image")
							  select images.Attribute("src").Value;

				editing.RemoveAll();

				try {
					editing.SetAttributeValue("background", ((ColorDrawable) carta.Background).Color.ToArgb().ToString());
					editing.SetAttributeValue("src", "");
				} catch {
					editing.SetAttributeValue("background", "");

					try {
						editing.SetAttributeValue("src", carta.GetTag(Resource.Id.imgSrc));
					} catch {
						editing.SetAttributeValue("src", "");
					}
				}

				for (int i = 0; i < carta.ChildCount; i++) {
					View current = carta.GetChildAt(i);

					if (IsImage(current)) {
						ImageView img = (ImageView) current;
						RelativeLayout.LayoutParams layout = (RelativeLayout.LayoutParams) img.LayoutParameters;

						XElement newImage = new XElement("Image");
						newImage.SetAttributeValue("rect", PxToDip(layout.LeftMargin) + "," + PxToDip(layout.TopMargin) + "," + PxToDip(layout.Width) + "," + PxToDip(layout.Height));
						newImage.SetAttributeValue("scale", img.GetScaleType().ToString());

						try {
							newImage.SetAttributeValue("background", ((ColorDrawable) img.Background).Color.ToArgb().ToString());
						} catch {
							newImage.SetAttributeValue("background", "");
						}

						newImage.SetAttributeValue("z", img.GetZ().ToString());

						try {
							newImage.SetAttributeValue("src", current.GetTag(Resource.Id.imgSrc));
						} catch {
							newImage.SetAttributeValue("src", "");
						}

						editing.Add(newImage);
					} else {
						EditText text = (EditText) current;
						RelativeLayout.LayoutParams layout = (RelativeLayout.LayoutParams) text.LayoutParameters;

						XElement newText = new XElement("Text");
						newText.SetAttributeValue("rect", PxToDip(layout.LeftMargin) + "," + PxToDip(layout.TopMargin) + "," + (PxToDip(text.MeasuredWidth) + 1) + "," + (PxToDip(text.MeasuredHeight) + 1));
						newText.SetAttributeValue("fontSize", PxToDip((int) text.TextSize).ToString());
						newText.SetAttributeValue("gravity", GetAlign(text.Gravity).ToString());
						newText.SetAttributeValue("style", text.Typeface.Style.ToString());
						newText.SetAttributeValue("color", text.CurrentTextColor.ToString());
						newText.SetAttributeValue("background", ((ColorDrawable) text.Background).Color.ToArgb().ToString());
						newText.SetAttributeValue("z", text.GetZ().ToString());
						newText.Value = text.Text;

						editing.Add(newText);
					}
				}

				notSavedImages.Clear();
				cartasXML.Save(xmlDir);
				Toast.MakeText(this, "Carta salva com sucesso!", ToastLength.Short).Show();
			} catch (Exception err) {
				Toast.MakeText(this, "Erro ao salvar a carta: " + err.Message, ToastLength.Long).Show();
			}

			if (nextAction == 1) {
				TurnAction();
			} else if (nextAction == 2) {
				Finish();
			}
		}

		public void CopyOtherSide () {
			if (selected != null) {
				selected = null;
				InvalidateOptionsMenu();
			}

			ReadInNewActivity(side, true);
		}

		public void InvertSides () {
			try {
				XDocument cartasXML = XDocument.Load(xmlDir);
				XElement xCarta = (from card in cartasXML.Element("Cartas").Elements("Carta")
								   where card.Attribute("id").Value == cardID.ToString()
								   select card).ToArray()[0];

				XElement oldFront = xCarta.Element("Frente");
				XElement oldBack = xCarta.Element("Verso");

				oldFront.Name = "Verso";
				oldBack.Name = "Frente";

				cartasXML.Save(xmlDir);

				ReadInNewActivity(side);
				Toast.MakeText(this, "Lados invertidos.", ToastLength.Long).Show();
			} catch (Exception err) {
				Toast.MakeText(this, "Erro ao inverter os lados: " + err.Message, ToastLength.Long).Show();
			}
		}

		public void CloneCard () {
			try {
				int length, toAdd = 0;
				string imgSrc, path, copied, extension, reqSrc;
				string fileDir = System.IO.Path.Combine(appPath, baralhoFileName.Replace(".lrv", " Resources"), "img.length");
				Dictionary<string, string> srcCopied = new Dictionary<string, string>();

				if (File.Exists(fileDir)) {
					StreamReader sr = new StreamReader(fileDir);
					int.TryParse(sr.ReadToEnd(), out length);
					sr.Close();
				} else {
					length = 0;
					StreamWriter sw = File.CreateText(fileDir);
					sw.Write(0);
					sw.Close();
				}

				XDocument cartasXML = XDocument.Load(xmlDir);
				XElement xCartaClone = new XElement((from card in cartasXML.Element("Cartas").Elements("Carta")
													 where card.Attribute("id").Value == cardID.ToString()
													 select card).ToArray()[0]);

				int nextCard = -1;
				int.TryParse(cartasXML.Element("Cartas").Element("Next").Value, out nextCard);

				xCartaClone.SetAttributeValue("id", nextCard);

				XElement[] xImages = (from img in xCartaClone.Descendants("Image").Concat(xCartaClone.Elements())
									  select img).ToArray();
				
				foreach (XElement img in xImages) {
					reqSrc = img.Attribute("src").Value;
					if (reqSrc.Length == 0)
						continue;

					imgSrc = "";
					foreach (KeyValuePair<string, string> src in srcCopied) {
						if (reqSrc == src.Key) {
							imgSrc = src.Value;
						}
					}

					if (imgSrc == "") {
						try {
							extension = reqSrc.Substring(reqSrc.LastIndexOf("."));
						} catch {
							extension = ".png";
						}

						imgSrc = "img-" + nextCard + "." + (length + toAdd) + extension;

						path = System.IO.Path.Combine(appPath, baralhoFileName.Replace(".lrv", " Resources"), reqSrc);
						copied = System.IO.Path.Combine(appPath, baralhoFileName.Replace(".lrv", " Resources"), imgSrc);

						File.Copy(path, copied);

						srcCopied.Add(reqSrc, imgSrc);
						toAdd++;
					}

					img.SetAttributeValue("src", imgSrc);
				}

				cartasXML.Element("Cartas").Add(xCartaClone);
				cartasXML.Element("Cartas").Element("Next").Value = (nextCard + 1).ToString();
				cartasXML.Save(xmlDir);

				if (toAdd > 0) {
					StreamWriter sw = new StreamWriter(fileDir);
					sw.Write(length + toAdd);
					sw.Close();
				}

				baralho.NCartas++;
				SGBD.UpdateBaralho(baralho);
				Toast.MakeText(this, "Carta clonada com sucesso.", ToastLength.Short).Show();

				Intent intent = new Intent(this, typeof(EditorCarta));
				intent.PutExtra("Baralho_ID", baralho.ID);
				intent.PutExtra("BaralhoFileName", baralhoFileName);
				intent.PutExtra("Card_ID", nextCard);
				intent.PutExtra("Side", side);
				intent.PutExtra("AppPath", appPath);
				StartActivity(intent);

				Finish();
			} catch (Exception err) {
				Toast.MakeText(this, "Erro ao clonar a carta: " + err.Message, ToastLength.Long).Show();
			}
		}

		public void DeleteCard () {
			try {
				string imgSrc;
				XDocument cartasXML = XDocument.Load(xmlDir);
				XElement xCarta = (from card in cartasXML.Element("Cartas").Elements("Carta")
								   where card.Attribute("id").Value == cardID.ToString()
								   select card).ToArray()[0];

				string[] xImages = (from img in xCarta.Descendants("Image").Concat(xCarta.Elements())
									  select img.Attribute("src").Value).ToArray();

				foreach (string img in xImages) {
					if (img.Length != 0) {
						imgSrc = System.IO.Path.Combine(appPath, baralhoFileName.Replace(".lrv", " Resources"), img);

						if (File.Exists(imgSrc)) {
							File.Delete(imgSrc);
						}
					}
				}

				xCarta.Remove();
				cartasXML.Save(xmlDir);

				baralho.NCartas--;
				SGBD.UpdateBaralho(baralho);

				Toast.MakeText(this, "Carta deletada com sucesso.", ToastLength.Short).Show();
				Finish();
			} catch (Exception err) {
				Toast.MakeText(this, "Erro ao deletar a carta: " + err.Message, ToastLength.Long).Show();
			}
		}

		public void CopyImageBitmap (string file, Bitmap img, Bitmap.CompressFormat format) {
			Stream output = File.Create(file);
			img.Compress(format, 100, output);
			output.Close();
		}

		public void ReadInNewActivity (string reqSide, bool copying = false) {
			Intent intent = new Intent(this, typeof(EditorCarta));
			intent.PutExtra("Baralho_ID", baralho.ID);
			intent.PutExtra("BaralhoFileName", baralhoFileName);
			intent.PutExtra("Card_ID", cardID);
			intent.PutExtra("Side", reqSide);
			intent.PutExtra("AppPath", appPath);
			intent.PutExtra("Copying", copying);
			StartActivity(intent);
			OverridePendingTransition(0, 0);

			Finish();
		}

		public void TurnAction () {
			ReadInNewActivity((side == "Frente") ? "Verso" : "Frente");
		}

		public void RefreshCard () {
			ReadInNewActivity(side);
		}

		public bool IsImage (View view = null) {
			if (view == null)
				view = selected;

			try {
				ImageView check = (ImageView) view;

				if (check != null)
					return true;
				else
					return false;

			} catch {
				return false;
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

		public void AddImage (Bitmap img, string src) {
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
			newImage.SetTag(Resource.Id.imgSrc, src);
			notSavedImages.Add(src);
			newImage.SetScaleType(ImageView.ScaleType.FitXy);
			newImage.LayoutParameters = layoutI;
			newImage.SetBackgroundColor(Color.Transparent);
			newImage.SetZ(0);
			newImage.Click += SelectI;

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
			requestAlert = 2;
			new AlertDialog.Builder(this)
					.SetTitle(Resource.String.ConfirmLeaveTitle)
					.SetMessage(Resource.String.ConfirmLeave)
					.SetPositiveButton(Resource.String.Salvar, this)
					.SetNeutralButton(Resource.String.NaoSalvar, this)
					.SetNegativeButton(Resource.String.Cancelar, (IDialogInterfaceOnClickListener) null)
					.Show();
		}

		public void SelectT (object sender, EventArgs e = null) {
			if (selected != null) {
				selected.Click -= SizeEvent;

				if (IsImage()) {
					selected.LongClick -= PosEvent;
					selected.Click += SelectI;
				} else {
					selected.Click += SelectT;
				}

				selected.ClearFocus();
				selected = null;
			}

			EditText clicked = (EditText) sender;

			selected = clicked;
			selected.RequestFocus();
			selected.Click -= SelectT;
			selected.Click += SizeEvent;

			InvalidateOptionsMenu();
		}

		public void SelectI (object sender, EventArgs e = null) {
			if (selected != null) {
				selected.Click -= SizeEvent;

				if (IsImage()) {
					selected.LongClick -= PosEvent;
					selected.Click += SelectI;
				} else {
					selected.Click += SelectT;
				}

				selected.ClearFocus();
				selected = null;
			}
			
			ImageView clicked = (ImageView) sender;

			selected = clicked;
			selected.RequestFocus();
			selected.Click -= SelectI;
			selected.Click += SizeEvent;
			selected.LongClick += PosEvent;

			InvalidateOptionsMenu();
		}

		public void Unselect (object sender = null, EventArgs e = null) {
			if (selected != null) {
				selected.Click -= SizeEvent;

				if (IsImage()) {
					selected.LongClick -= PosEvent;
					selected.Click += SelectI;
				} else {
					selected.Click += SelectT;
				}

				selected.ClearFocus();
				selected = null;

				title.RequestFocus();
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
				if (IsImage()) {
					ImageView image = (ImageView) selected;
					Bitmap originalBitmap = ((BitmapDrawable) image.Drawable).Bitmap;
					Bitmap newBitmap;

					try {
						newBitmap = originalBitmap.Copy(originalBitmap.GetConfig(), true);
						images.Add(newBitmap);
					} catch {
						Toast.MakeText(this, "Erro ao carregar imagem: limite de espaço da memória alcançado.", ToastLength.Long).Show();
						return;
					}

					ImageView newImage = new ImageView(carta.Context);

					//Cria as configurações de tamanho e posicionamento da Imagem
					RelativeLayout.LayoutParams layoutI = new RelativeLayout.LayoutParams(image.LayoutParameters.Width, image.LayoutParameters.Height);
					layoutI.LeftMargin = ((RelativeLayout.LayoutParams) image.LayoutParameters).LeftMargin + 5;
					layoutI.TopMargin = ((RelativeLayout.LayoutParams) image.LayoutParameters).TopMargin + 5;

					//Copia as configurações para o novo ImageView
					newImage.SetImageBitmap(newBitmap);
					newImage.SetTag(Resource.Id.imgSrc, selected.GetTag(Resource.Id.imgSrc));
					newImage.SetScaleType(image.GetScaleType());
					newImage.LayoutParameters = layoutI;
					newImage.SetBackgroundColor(((ColorDrawable) image.Background).Color);
					newImage.SetZ(image.GetZ());
					newImage.Click += SelectI;

					carta.AddView(newImage);
					SelectI(newImage, null);
				} else {
					EditText label = (EditText) selected;
					EditText newLabel = new EditText(carta.Context);


					//Cria as configurações de tamanho e posicionamento do Texto
					RelativeLayout.LayoutParams layoutT = new RelativeLayout.LayoutParams(label.MeasuredWidth, label.MeasuredHeight);
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
				}
			}
		}

		public void Focus (object sender, View.FocusChangeEventArgs e) {
			if (e.HasFocus && sender != (object) selected) {
				SelectT(sender);
			}
		}

		public void ChangeFontSize (object sender, AfterTextChangedEventArgs e) {
			if (selected != null && !IsImage()) {
				EditText label = (EditText)selected;
				try {
					label.TextSize = int.Parse(FindViewById<AutoCompleteTextView>(Resource.Id.fontSize).Text);
				} catch {
					Toast.MakeText(this, "Valor inválido.", ToastLength.Short).Show();
				}
			}
		}

		public void Negrito (object sender, EventArgs e) {
			if (selected != null && !IsImage()) {
				EditText label = (EditText)selected;
				Button clicked = (Button)sender;

				switch (label.Typeface.Style) {
					case TypefaceStyle.Bold:
						label.SetTypeface(Typeface.SansSerif, TypefaceStyle.Normal);
						clicked.SetBackgroundColor(new Color(240, 223, 238)); //Disable
						break;
					case TypefaceStyle.BoldItalic:
						label.SetTypeface(label.Typeface, TypefaceStyle.Italic);
						clicked.SetBackgroundColor(new Color(240, 223, 238)); //Disable
						break;
					case TypefaceStyle.Italic:
						label.SetTypeface(label.Typeface, TypefaceStyle.BoldItalic);
						clicked.SetBackgroundColor(new Color(255, 235, 255)); //Enable
						break;
					case TypefaceStyle.Normal:
						label.SetTypeface(label.Typeface, TypefaceStyle.Bold);
						clicked.SetBackgroundColor(new Color(255, 235, 255)); //Enable
						break;
				}
			}
		}

		public void Italico (object sender, EventArgs e) {
			if (selected != null && !IsImage()) {
				EditText label = (EditText)selected;
				Button clicked = (Button)sender;

				switch (label.Typeface.Style) {
					case TypefaceStyle.Bold:
						label.SetTypeface(label.Typeface, TypefaceStyle.BoldItalic);
						clicked.SetBackgroundColor(new Color(255, 235, 255));
						break;
					case TypefaceStyle.BoldItalic:
						label.SetTypeface(label.Typeface, TypefaceStyle.Bold);
						clicked.SetBackgroundColor(new Color(240, 223, 238));
						break;
					case TypefaceStyle.Italic:
						label.SetTypeface(Typeface.SansSerif, TypefaceStyle.Normal);
						clicked.SetBackgroundColor(new Color(240, 223, 238));
						break;
					case TypefaceStyle.Normal:
						label.SetTypeface(label.Typeface, TypefaceStyle.Italic);
						clicked.SetBackgroundColor(new Color(255, 235, 255));
						break;
				}
			}
		}

		public void ChangeAlign (object sender, AdapterView.ItemSelectedEventArgs e) {
			if (selected != null && !IsImage()) {
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
			if (selected != null && IsImage()) {
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

		public void CancelColor (object sender = null, EventArgs e = null) {
			//Voltar ao original
			if (request == 1)
				((EditText) selected).SetTextColor(originalColor);
			else if (request == 2 || request == 3)
				selected.SetBackgroundColor(originalColor);
			else if (request == 4)
				carta.SetBackgroundColor(originalColor);

			request = 0;
			currentDialog = 0;
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

		public GravityFlags GetAlign (int gravity) {
			switch (gravity) {
				case 0:
					return GravityFlags.Left;
				case 1:
					return GravityFlags.CenterHorizontal;
				case 2:
					return GravityFlags.Right;
				case 3:
					return GravityFlags.CenterVertical;
				case 4:
					return GravityFlags.Center;
				case 5:
					return GravityFlags.CenterVertical | GravityFlags.Right;
				case 6:
					return GravityFlags.Bottom;
				case 7:
					return GravityFlags.Bottom | GravityFlags.CenterHorizontal;
				case 8:
					return GravityFlags.Bottom | GravityFlags.Right;
				default:
					return GravityFlags.Start;
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
				case "bolditalic":
					return TypefaceStyle.BoldItalic;
				case "bold":
					return TypefaceStyle.Bold;
				case "italic":
					return TypefaceStyle.Italic;
				default:
					return TypefaceStyle.Normal;
			}
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
				currentDialog = 1;
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

				FindViewById<Button>(Resource.Id.confirmPos).Click += delegate { InvalidateOptionsMenu(); };
				currentDialog = 2;
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

				if (IsImage()) {
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
				currentDialog = 2;
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
			if (resultCode == Result.Ok && data.Data != null) {
				try {
					int length;
					string imgSrc, path, copied;
					string fileDir = System.IO.Path.Combine(appPath, baralhoFileName.Replace(".lrv", " Resources"), "img.length");

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

					try {
						ICursor cursor = ContentResolver.Query(data.Data, new string[] { MediaStore.MediaColumns.Data }, null, null, null);

						if (cursor.MoveToFirst()) {
							int index = cursor.GetColumnIndexOrThrow(MediaStore.MediaColumns.Data);
							path = cursor.GetString(index);
							cursor.Close();

							string extension;
							try {
								extension = path.Substring(path.LastIndexOf("."));
							} catch {
								extension = ".png";
							}

							imgSrc = "img-" + cardID + side[0] + "." + length + extension;
							copied = System.IO.Path.Combine(appPath, baralhoFileName.Replace(".lrv", " Resources"), imgSrc);

							File.Copy(path, copied);

							try {
								BitmapFactory.Options options = new BitmapFactory.Options();
								options.InJustDecodeBounds = true;

								BitmapFactory.DecodeFile(copied, options);
								options.InSampleSize = CalculateInSample(options, DipToPx(310), DipToPx(310));
								options.InJustDecodeBounds = false;

								Bitmap result = BitmapFactory.DecodeFile(copied, options);
								images.Add(result);

								if (requestCode == 1) {
									AddImage(result, imgSrc);
								} else if (requestCode == 2 && IsImage()) {
									((ImageView) selected).SetImageBitmap(result);
									((ImageView) selected).SetTag(Resource.Id.imgSrc, imgSrc);
								}
							} catch {
								throw new Exception("limite de espaço da memória alcançado.");
							}
						} else {
							throw new Exception("cursor vazio.");
						}
					} catch (Exception err) {
						Toast.MakeText(this, "Erro ao carregar imagem: " + err.Message, ToastLength.Long).Show();
					}
				} catch (Exception err) {
					Toast.MakeText(this, "R!: " + err.Message, ToastLength.Long).Show();
				}
			}

			base.OnActivityResult(requestCode, resultCode, data);
		}

		public void DeleteImage (string imgSrc) {
			if (imgSrc.Length >= 0)
				toDelete.Add(imgSrc);
		}

		public void OnClick (IDialogInterface dialog, int which) {
			if (requestAlert == 1) {
				request = 4;

				try {
					originalColor = ((ColorDrawable) carta.Background).Color;
				} catch {
					carta.SetBackgroundColor(Color.White);
					originalColor = Color.White;
				}

				ColorView(originalColor);
			} else if (requestAlert == 2) {
				if (which == (int) DialogButtonType.Positive) {
					SaveCard(1);
				} else if (which == (int) DialogButtonType.Neutral) {
					TurnAction();
				}
			} else if (requestAlert == 3) {
				RefreshCard();
			} else if (requestAlert == 4) {
				DeleteCard();
			} else if (requestAlert == 5) {
				CopyOtherSide();
			} else if (requestAlert == 6) {
				CloneCard();
			} else if (requestAlert == 7) {
				if (which == (int) DialogButtonType.Positive) {
					SaveCard(2);
				} else if (which == (int) DialogButtonType.Neutral) {
					Finish();
				}
			} else if (requestAlert == 8) {
				InvertSides();
			}

			requestAlert = 0;
		}

		public override bool OnCreateOptionsMenu (IMenu menu) {
			if (selected == null) {
				MenuInflater.Inflate(Resource.Menu.EditorCartaMenu, menu);

				if (relativeLayout == null)
					relativeLayout = FindViewById<RelativeLayout>(Resource.Id.relativeLayout1);

				relativeLayout.RemoveView(FindViewById<GridLayout>(Resource.Id.gridLayout1));
				LayoutInflater.Inflate(Resource.Layout.DefaultTools, relativeLayout);

				if (side == "Frente")
					FindViewById<Button>(Resource.Id.EditarVerso).Text = GetString(Resource.String.EditarVerso);
				else
					FindViewById<Button>(Resource.Id.EditarVerso).Text = GetString(Resource.String.EditarFrente);

				FindViewById<Button>(Resource.Id.AddText).Click += AddText;
				FindViewById<Button>(Resource.Id.EditarVerso).Click += Turn;

				FindViewById<Button>(Resource.Id.AddImg).Click += delegate { AddImg(false); };
				FindViewById<Button>(Resource.Id.BackgroundColor).Click += delegate {
					try {
						originalColor = ((ColorDrawable) carta.Background).Color;

						request = 4;
						ColorView(originalColor);
					} catch {
						requestAlert = 1;
						new AlertDialog.Builder(this)
							.SetTitle(Resource.String.ConfirmBackgroundTitle)
							.SetMessage(Resource.String.ConfirmBackground)
							.SetPositiveButton(Resource.String.Yes, this)
							.SetNegativeButton(Resource.String.No, (IDialogInterfaceOnClickListener) null)
							.Show();
					}
				};
				currentDialog = 0;
			} else if (IsImage()) {
				MenuInflater.Inflate(Resource.Menu.ImageSelected, menu);

				if (relativeLayout == null)
					relativeLayout = FindViewById<RelativeLayout>(Resource.Id.relativeLayout1);
				
				selected.Id = 911;
				relativeLayout.RemoveView(FindViewById<GridLayout>(Resource.Id.gridLayout1)); //Unselect on draw

				selected = FindViewById(911);
				selected.Id = 0;

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
					currentDialog = 0;
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

				try {
					LayoutInflater.Inflate(Resource.Layout.LabelTools, relativeLayout);

					AutoCompleteTextView fontS = FindViewById<AutoCompleteTextView>(Resource.Id.fontSize);
					fontS.Adapter = new ArrayAdapter(this, Android.Resource.Layout.SimpleDropDownItem1Line, new string[] { "8", "9", "10", "11", "12", "14", "16", "18", "20", "22", "24", "26", "28", "32", "36", "42", "48", "72" });
					fontS.Text = PxToDip((int) ((EditText) selected).TextSize).ToString();
					fontS.AfterTextChanged += ChangeFontSize;

					aligns = new string[] { "Superior-Esquerdo", "Superior-Centro", "Superior-Direito", "Centro-Esquerdo", "Centro-Centro", "Centro-Direito", "Inferior-Esquerdo", "Inferior-Centro", "Inferior-Direito" };

					Spinner align = FindViewById<Spinner>(Resource.Id.align);
					align.Adapter = new ArrayAdapter(this, Android.Resource.Layout.SimpleDropDownItem1Line, aligns);
					align.SetSelection(GetAlign(((EditText) selected).Gravity));
					align.ItemSelected += ChangeAlign;

					if (((EditText) selected).Typeface == null) {
						((EditText) selected).Typeface = Typeface.SansSerif;
					}

					TypefaceStyle style = ((EditText) selected).Typeface.Style;

					Button negrito = FindViewById<Button>(Resource.Id.negrito);
					negrito.Click += Negrito;
					if (style == TypefaceStyle.Bold || style == TypefaceStyle.BoldItalic)
						negrito.SetBackgroundColor(new Color(255, 235, 255));
					else
						negrito.SetBackgroundColor(new Color(240, 223, 238));

					Button italico = FindViewById<Button>(Resource.Id.itálico);
					italico.Click += Italico;
					if (style == TypefaceStyle.Italic || style == TypefaceStyle.BoldItalic)
						italico.SetBackgroundColor(new Color(255, 235, 255));
					else
						italico.SetBackgroundColor(new Color(240, 223, 238));

					FindViewById<Button>(Resource.Id.duplicar).Click += Duplicar;
					currentDialog = 0;
				} catch (Exception err) {
					Toast.MakeText(this, "!: " + err.Message, ToastLength.Long).Show();
				}
			}

			return base.OnCreateOptionsMenu(menu);
		}

		public override bool OnOptionsItemSelected (IMenuItem item) {
			switch (item.ItemId) {
				case Android.Resource.Id.Home:
					requestAlert = 7;
					new AlertDialog.Builder(this)
							.SetTitle(Resource.String.ConfirmLeaveTitle)
							.SetMessage(Resource.String.ConfirmLeave)
							.SetPositiveButton(Resource.String.Salvar, this)
							.SetNeutralButton(Resource.String.NaoSalvar, this)
							.SetNegativeButton(Resource.String.Cancelar, (IDialogInterfaceOnClickListener) null)
							.Show();
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
					if (IsImage()) {
						try {
							((BitmapDrawable) ((ImageView) selected).Drawable).Bitmap.Recycle();
							((ImageView) selected).SetImageBitmap(null);
							DeleteImage(selected.GetTag(Resource.Id.imgSrc).ToString());
						} catch { }
					}
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
					try {
						((BitmapDrawable) ((ImageView) selected).Drawable).Bitmap.Recycle();
						((ImageView) selected).SetImageBitmap(null);
						DeleteImage(selected.GetTag(Resource.Id.imgSrc).ToString());
						selected.SetTag(Resource.Id.imgSrc, "");
					} catch { }
					break;
				case Resource.Id.UseAsBackground:
					try {
						if (((BitmapDrawable) ((ImageView) selected).Drawable).Bitmap != null) {
							//Set
							BitmapDrawable newBackground = (BitmapDrawable) ((ImageView) selected).Drawable;
							carta.Background = newBackground;
							carta.SetTag(Resource.Id.imgSrc, selected.GetTag(Resource.Id.imgSrc));

							//Clean
							carta.RemoveView(selected);
							Unselect();
						} else {
							Toast.MakeText(this, "Imagem limpa, por favor troque-a", ToastLength.Short).Show();
						}
					} catch {
						Toast.MakeText(this, "Imagem limpa, por favor troque-a", ToastLength.Short).Show();
					}
					break;
				case Resource.Id.ResetBackground:
					try {
						((BitmapDrawable) carta.Background).Bitmap.Recycle();
						DeleteImage(carta.GetTag(Resource.Id.imgSrc).ToString());
					} catch { }

					carta.SetBackgroundColor(Color.White);
					carta.SetTag(Resource.Id.imgSrc, "");
					break;
				case Resource.Id.HorizontalCenter:
					HorizontalCenter();
					break;
				case Resource.Id.VerticalCenter:
					VerticalCenter();
					break;
				case Resource.Id.CopiarLado:
					requestAlert = 5;
					new AlertDialog.Builder(this)
						.SetTitle(Resource.String.ConfirmCopyTitle)
						.SetMessage(Resource.String.ConfirmCopy)
						.SetPositiveButton(Resource.String.Yes, this)
						.SetNegativeButton(Resource.String.No, (IDialogInterfaceOnClickListener) null)
						.Show();
					break;
				case Resource.Id.InverterLado:
					requestAlert = 8;
					new AlertDialog.Builder(this)
						.SetTitle(Resource.String.ConfirmInvertTitle)
						.SetMessage(Resource.String.ConfirmInvert)
						.SetPositiveButton(Resource.String.Yes, this)
						.SetNegativeButton(Resource.String.No, (IDialogInterfaceOnClickListener) null)
						.Show();
					break;
				case Resource.Id.RecarregarCarta:
					requestAlert = 3;
					new AlertDialog.Builder(this)
						.SetTitle(Resource.String.ConfirmRefreshTitle)
						.SetMessage(Resource.String.ConfirmRefresh)
						.SetPositiveButton(Resource.String.Yes, this)
						.SetNegativeButton(Resource.String.No, (IDialogInterfaceOnClickListener) null)
						.Show();
					break;
				case Resource.Id.SalvarCarta:
					SaveCard();
					break;
				case Resource.Id.ExcluirCarta:
					requestAlert = 4;
					new AlertDialog.Builder(this)
						.SetTitle(Resource.String.ConfirmExclusionTitle)
						.SetMessage(Resource.String.ConfirmExclusion)
						.SetPositiveButton(Resource.String.Yes, this)
						.SetNegativeButton(Resource.String.No, (IDialogInterfaceOnClickListener) null)
						.Show();
					break;
				case Resource.Id.ClonarCarta:
					requestAlert = 6;
					new AlertDialog.Builder(this)
						.SetTitle(Resource.String.ConfirmCloneTitle)
						.SetMessage(Resource.String.ConfirmClone)
						.SetPositiveButton(Resource.String.Yes, this)
						.SetNegativeButton(Resource.String.No, (IDialogInterfaceOnClickListener) null)
						.Show();
					break;
				default:
					return base.OnOptionsItemSelected(item);
			}

			return true;
		}

		public override void OnBackPressed () {
			if (currentDialog == 1) {
				CancelColor();
			} else if (currentDialog == 2) {
				InvalidateOptionsMenu();
			} else if (selected != null) {
				Unselect();
			} else {
				requestAlert = 7;
				new AlertDialog.Builder(this)
						.SetTitle(Resource.String.ConfirmLeaveTitle)
						.SetMessage(Resource.String.ConfirmLeave)
						.SetPositiveButton(Resource.String.Salvar, this)
						.SetNeutralButton(Resource.String.NaoSalvar, this)
						.SetNegativeButton(Resource.String.Cancelar, (IDialogInterfaceOnClickListener) null)
						.Show();
			}
		}

		public int CalculateInSample (BitmapFactory.Options options, int width, int height) {
			int result = 1;

			if (options.OutWidth > width || options.OutHeight > height) {
				int halfWidth = options.OutWidth / 2;
				int halfHeight = options.OutHeight / 2;

				while (halfWidth / result > width && halfHeight / result > height) {
					result *= 2;
				}
			}

			return result;
		}

		protected override void OnDestroy () {
			for (int i = 0; i < images.Count; i++) {
				if (images[i] != null) {
					images[i].Recycle();
					images[i] = null;
				}
			}

			bool continuar;
			object tag;
			foreach (string imgSrc in toDelete.Concat(notSavedImages)) {
				continuar = false;

				for (int i = 0; i < carta.ChildCount; i++) {
					tag = carta.GetChildAt(i).GetTag(Resource.Id.imgSrc);
					if (tag == null)
						continue;

					if (tag.ToString() == imgSrc && !notSavedImages.Contains(imgSrc)) {
						continuar = true;
						break;
					}
				}

				tag = carta.GetTag(Resource.Id.imgSrc);
				if (tag != null) {
					if (tag.ToString() == imgSrc && !notSavedImages.Contains(imgSrc)) {
						continuar = true;
					}
				}

				if (continuar)
					continue;

				string file = System.IO.Path.Combine(appPath, baralhoFileName.Replace(".lrv", " Resources"), imgSrc);
				if (File.Exists(file)) {
					File.Delete(file);
				}
			}

			base.OnDestroy();
		}
	}
}
 