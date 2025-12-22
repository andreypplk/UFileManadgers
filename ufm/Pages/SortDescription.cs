namespace ufm
{
    public class SortDescription
    {
        public string PropertyName { get; set; }
        public string DisplayName { get; set; }

        public override string ToString() => DisplayName;
    }
}