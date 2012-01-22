// Copyright (C) 2010 Steffen Rendle, Zeno Gantner
// Copyright (C) 2011, 2012 Zeno Gantner
//
// This file is part of MyMediaLite.
//
// MyMediaLite is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// MyMediaLite is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with MyMediaLite.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using MyMediaLite.Data;
using MyMediaLite.DataType;

namespace MyMediaLite.ItemRecommendation
{
	/// <summary>Class that contains static methods for item prediction</summary>
	public static class Extensions
	{
		/// <summary>Write item predictions (scores) to a file</summary>
		/// <param name="recommender">the <see cref="IRecommender"/> to use for making the predictions</param>
		/// <param name="train">a user-wise <see cref="IPosOnlyFeedback"/> containing the items already observed</param>
		/// <param name="candidate_items">the list of candidate items</param>
		/// <param name="num_predictions">the number of items to return per user, -1 if there should be no limit</param>
		/// <param name="filename">the name of the file to write to</param>
		/// <param name="users">a list of users to make recommendations for</param>
		/// <param name="user_mapping">an <see cref="IEntityMapping"/> object for the user IDs</param>
		/// <param name="item_mapping">an <see cref="IEntityMapping"/> object for the item IDs</param>
		static public void WritePredictions(
			this IRecommender recommender,
			IPosOnlyFeedback train,
			ICollection<int> candidate_items,
			int num_predictions,
			string filename,
			IList<int> users = null,
			IEntityMapping user_mapping = null, IEntityMapping item_mapping = null)
		{
			using (var writer = new StreamWriter(filename))
				WritePredictions(recommender, train, candidate_items, num_predictions, writer, users, user_mapping, item_mapping);
		}

		/// <summary>Write item predictions (scores) to a TextWriter object</summary>
		/// <param name="recommender">the <see cref="IRecommender"/> to use for making the predictions</param>
		/// <param name="train">a user-wise <see cref="IPosOnlyFeedback"/> containing the items already observed</param>
		/// <param name="candidate_items">the list of candidate items</param>
		/// <param name="num_predictions">the number of items to return per user, -1 if there should be no limit</param>
		/// <param name="writer">the <see cref="TextWriter"/> to write to</param>
		/// <param name="users">a list of users to make recommendations for; if null, all users in train will be provided with recommendations</param>
		/// <param name="user_mapping">an <see cref="IEntityMapping"/> object for the user IDs</param>
		/// <param name="item_mapping">an <see cref="IEntityMapping"/> object for the item IDs</param>
		static public void WritePredictions(
			this IRecommender recommender,
			IPosOnlyFeedback train,
			ICollection<int> candidate_items,
			int num_predictions,
			TextWriter writer,
			IList<int> users = null,
			IEntityMapping user_mapping = null, IEntityMapping item_mapping = null)
		{
			users = new List<int>(train.AllUsers);

			foreach (int user_id in users)
			{
				var ignore_items = train.UserMatrix[user_id];
				WritePredictions(recommender, user_id, candidate_items, ignore_items, num_predictions, writer, user_mapping, item_mapping);
			}
		}

		/// <summary>Write item predictions (scores) to a TextWriter object</summary>
		/// <param name="recommender">the <see cref="IRecommender"/> to use for making the predictions</param>
		/// <param name="user_id">the ID of the user to make recommendations for</param>
		/// <param name="candidate_items">the list of candidate items</param>
		/// <param name="ignore_items">a list of items for which no predictions should be made</param>
		/// <param name="num_predictions">the number of items to return per user, -1 if there should be no limit</param>
		/// <param name="writer">the <see cref="TextWriter"/> to write to</param>
		/// <param name="user_mapping">an <see cref="IEntityMapping"/> object for the user IDs</param>
		/// <param name="item_mapping">an <see cref="IEntityMapping"/> object for the item IDs</param>
		static public void WritePredictions(
			this IRecommender recommender,
			int user_id,
			ICollection<int> candidate_items,
			ICollection<int> ignore_items,
			int num_predictions,
			TextWriter writer,
			IEntityMapping user_mapping, IEntityMapping item_mapping)
		{
			if (user_mapping == null)
				user_mapping = new IdentityMapping();
			if (item_mapping == null)
				item_mapping = new IdentityMapping();

			var score_list = new List<WeightedItem>();
			foreach (int item_id in candidate_items)
				score_list.Add( new WeightedItem(item_id, recommender.Predict(user_id, item_id)));
			score_list = score_list.OrderByDescending(x => x.weight).ToList();

			int prediction_count = 0;
			writer.Write("{0}\t[", user_mapping.ToOriginalID(user_id));
			foreach (var wi in score_list)
			{
				if (!ignore_items.Contains(wi.item_id) && wi.weight > double.MinValue)
				{
					if (prediction_count == 0)
						writer.Write("{0}:{1}", item_mapping.ToOriginalID(wi.item_id), wi.weight.ToString(CultureInfo.InvariantCulture));
					else
						writer.Write(",{0}:{1}", item_mapping.ToOriginalID(wi.item_id), wi.weight.ToString(CultureInfo.InvariantCulture));

					prediction_count++;
				}

				if (prediction_count == num_predictions)
					break;
			}
			writer.WriteLine("]");
		}
	}
}