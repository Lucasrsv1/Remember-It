using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace Remember_It {
	class CardsManager : BaseAdapter<string> {
		private List<string> Dados;
		private Activity C;

		public CardsManager (List<string> dados, Activity c) {
			Dados = dados;
			C = c;
		}

		public override string this[int position] {
			get {
				return Dados[position];
			}
		}

		public override int Count {
			get {
				return Dados.Count;
			}
		}

		public override long GetItemId (int position) {
			return position;
		}

		public override View GetView (int position, View convertView, ViewGroup parent) {
			if (convertView == null)
				convertView = C.LayoutInflater.Inflate(Resource.Layout.CartaCelula, null);

			convertView.FindViewById<TextView>(Resource.Id.textView).Text = Dados[position];
			return convertView;
		}
	}
}