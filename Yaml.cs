using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Yaml
{
    public class Document {
        public Document(Node[] nodes) {
            Nodes = nodes;
        }

        public Node[] Nodes { get; set; }

        public override string ToString()
        {
            return Nodes.Aggregate(new StringBuilder("---\r\n"),
                (sb, n) => sb.Append(n.Print(new Indent("  "))),
                sb => sb.ToString());
        }
    }

    public class Mapping : IEnumerable<Node>
    {
        private List<Node> nodes = new List<Node>();

        public Mapping Add(string name, string value)
        {
            nodes.Add(new TextNode(name, value.Trim()));
            return this;
        }

        public Mapping Add(string name, List<string> value)
        {
            nodes.Add(new ListNode(name, value.Select(s => s.Trim()).ToList()));
            return this;
        }

        public Mapping Add(string name, Mapping value)
        {
            nodes.Add(new HashNode(name, value));
            return this;
        }

        #region IEnumerable<Node> Members

        public IEnumerator<Node> GetEnumerator()
        {
            return nodes.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }

    public abstract class Node
    {
        public string Name { get; set; }

        internal abstract string Print(Indent i);
    }

    public class HashNode : Node
    {
        public HashNode(string name, Mapping value)
        {
            Name = name;
            Value = value;
        }

        public Mapping Value { get; set; }

        internal override string Print(Indent i)
        {
            return Value.Aggregate(new StringBuilder(Name + ":\r\n"),
                (sb, n) => sb.Append(string.Format("{0}{1}", i+1, n.Print(i+1))),
                sb => sb.ToString());
        }
    }

    public class ListNode : Node
    {
        public ListNode(string name, List<string> value)
        {
            Name = name;
            Value = value;
        }

        public List<string> Value { get; set; }

        internal override string Print(Indent i)
        {
            return Value.Aggregate(new StringBuilder(Name + ":\r\n"),
                (sb, s) => sb.AppendLine(string.Format("{0}- \"{1}\"", i, s)),
                sb => sb.ToString());
        }
    }

    public class TextNode : Node
    {
        public TextNode(string name, string value) {
            Name = name;
            Value = value;
        }

        public string Value { get; set; }

        internal override string Print(Indent i) {
            return string.Format("{0}: \"{1}\"\r\n", Name, Value);
        }
    }

    internal class Indent
    {
        public Indent(string indentString) : this(indentString, 0) {}

        private Indent(string indentString, int indentLevel) {
            IndentString = indentString;
            IndentLevel = indentLevel;
        }

        public string IndentString { get; set; }
        public int IndentLevel { get; set; }

        public static Indent operator+(Indent i, int n) {
            return new Indent(i.IndentString, i.IndentLevel + n);
        }

        public override string ToString()
        {
            var ret = new StringBuilder();
            for (int i = 0; i < IndentLevel; ++i)
                ret.Append(IndentString);
            return ret.ToString();
        }
    }
}
