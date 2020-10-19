﻿using System.Collections.Generic;
using System.Linq;

namespace Hollywood.Runtime.Internal
{
	public class Hierarchy<T> where T : class
	{
		private Dictionary<T, HashSet<T>> ParentToChildren = new Dictionary<T, HashSet<T>>();
		private Dictionary<T, T> ChildToParent = new Dictionary<T, T>();

		public void Add(T element, T parent = null)
		{
			if (parent != null && !Contains(parent))
			{
				Add(parent);
			}

			Assert.IsTrue(!Contains(element) || GetParent(element) == null);

			ChildToParent[element] = parent ?? element;

			if (parent != null)
			{
				if (!ParentToChildren.TryGetValue(parent, out HashSet<T> parentChildren))
				{
					parentChildren = new HashSet<T>();
					ParentToChildren.Add(parent, parentChildren);
				}

				parentChildren.Add(element);
			}
		}

		public void Remove(T element, bool recursively = false)
		{
			Assert.IsTrue(Contains(element));
			Assert.IsTrue(!recursively || GetChildren(element).Count() == 0);

			if (recursively)
			{
				if (ParentToChildren.TryGetValue(element, out HashSet<T> elementChildren))
				{
					// Can't use foreach since Remove modifies elementChildren internally
					while (elementChildren.Count > 0)
					{
						var child = elementChildren.First();
						Remove(child, recursively);
					}
				}
			}
			ParentToChildren.Remove(element);

			T parent = ChildToParent[element];
			if (parent != element && ParentToChildren.TryGetValue(parent, out HashSet<T> parentChildren))
			{
				parentChildren.Remove(element);
			}
			ChildToParent.Remove(element);
		}

		public IEnumerable<T> GetChildren(T parent)
		{
			if (!Contains(parent) || !ParentToChildren.TryGetValue(parent, out var children))
			{
				return Enumerable.Empty<T>();
			}

			return children;
		}

		public T GetParent(T child)
		{
			Assert.IsTrue(Contains(child));

			T parent = ChildToParent[child];

			return parent != child ? parent : null;
		}

		public bool Contains(T element)
		{
			Assert.IsNotNull(element);

			return ChildToParent.ContainsKey(element);
		}

		public void Reset()
		{
			ParentToChildren.Clear();
			ChildToParent.Clear();
		}
	}
}