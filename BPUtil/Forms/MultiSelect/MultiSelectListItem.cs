namespace BPUtil.Forms.MultiSelect
{
	public class MultiSelectListItem<T>
	{
		public readonly string Key;
		public readonly T Value;
		public MultiSelectListItem()
		{
		}
		public MultiSelectListItem(string key, T value)
		{
			Key = key;
			Value = value;
		}
		public override string ToString()
		{
			return Key;
		}
	}
}
