#if false

// Accord Machine Learning Library
// The Accord.NET Framework
// http://accord-framework.net
//
// Copyright © César Souza, 2009-2014
// cesarsouza at gmail.com
//
//    This library is free software; you can redistribute it and/or
//    modify it under the terms of the GNU Lesser General Public
//    License as published by the Free Software Foundation; either
//    version 2.1 of the License, or (at your option) any later version.
//
//    This library is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//    Lesser General Public License for more details.
//
//    You should have received a copy of the GNU Lesser General Public
//    License along with this library; if not, write to the Free Software
//    Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
//

using System;
using System.Collections.Generic;
//using Accord.Math;
//using AForge;
using Parallel = System.Threading.Tasks.Parallel;

	
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
//using Accord.MachineLearning.DecisionTrees.Rules;


using System;
using System.Runtime.Serialization;
using System.Collections.Generic;


using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TextCharacteristicLearner
{
	//public static class InvalidOperationException : Extension





    public enum ComparisonKind
    {
        None,

        Equal,

        NotEqual,

        GreaterThanOrEqual,

        GreaterThan,

        LessThan,

        LessThanOrEqual
    }

    /// <summary>
    ///   Extension methods for <see cref="ComparisonKind"/> enumeration values.
    /// </summary>
    /// 
    public static class ComparisonExtensions
    {

        /// <summary>
        ///   Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// 
        /// <param name="comparison">The comparison type.</param>
        /// 
        /// <returns>
        ///   A <see cref="System.String"/> that represents this instance.
        /// </returns>
        /// 
        public static string ToString(this ComparisonKind comparison)
        {
            switch (comparison)
            {
                case ComparisonKind.Equal:
                    return "==";

                case ComparisonKind.GreaterThan:
                    return ">";

                case ComparisonKind.GreaterThanOrEqual:
                    return ">=";

                case ComparisonKind.LessThan:
                    return "<";

                case ComparisonKind.LessThanOrEqual:
                    return "<=";

                case ComparisonKind.NotEqual:
                    return "!=";

                default:
                    throw new Exception("Unexpected node comparison type.");					
//                    throw new InvalidOperationException("Unexpected node comparison type.");
            }
        }
    }



	    public enum DecisionVariableKind
    {
        /// <summary>
        ///   Attribute is discrete-valued.
        /// </summary>
        /// 
        Discrete,

        /// <summary>
        ///   Attribute is continuous-valued.
        /// </summary>
        /// 
        Continuous
    }


    /// <summary>
    ///   Decision attribute.
    /// </summary>
    /// 
    [Serializable]
    public class DecisionVariable
    {
        /// <summary>
        ///   Gets the name of the attribute.
        /// </summary>
        /// 
        public string Name { get; private set; }

        /// <summary>
        ///   Gets the nature of the attribute (i.e. real-valued or discrete-valued).
        /// </summary>
        /// 
        public DecisionVariableKind Nature { get; private set; }

        /// <summary>
        ///   Gets the valid range of the attribute.
        /// </summary>
        /// 
        public DoubleRange Range { get; private set; }


        /// <summary>
        ///   Creates a new <see cref="DecisionVariable"/>.
        /// </summary>
        /// 
        /// <param name="name">The name of the attribute.</param>
        /// <param name="range">The range of valid values for this attribute. Default is [0;1].</param>
        /// 
        public DecisionVariable(string name, DoubleRange range)
        {
            this.Name = name;
            this.Nature = DecisionVariableKind.Continuous;
            this.Range = range;
        }

        /// <summary>
        ///   Creates a new <see cref="DecisionVariable"/>.
        /// </summary>
        /// 
        /// <param name="name">The name of the attribute.</param>
        /// <param name="nature">The attribute's nature (i.e. real-valued or discrete-valued).</param>
        /// 
        public DecisionVariable(string name, DecisionVariableKind nature)
        {
            this.Name = name;
            this.Nature = nature;
            this.Range = new DoubleRange(0, 1);
        }

        /// <summary>
        ///   Creates a new <see cref="DecisionVariable"/>.
        /// </summary>
        /// 
        /// <param name="name">The name of the attribute.</param>
        /// <param name="range">The range of valid values for this attribute.</param>
        /// 
        public DecisionVariable(string name, IntRange range)
        {
            this.Name = name;
            this.Nature = DecisionVariableKind.Discrete;
            this.Range = new DoubleRange(range.Min, range.Max);
        }

        /// <summary>
        ///   Creates a new discrete-valued <see cref="DecisionVariable"/>.
        /// </summary>
        /// 
        /// <param name="name">The name of the attribute.</param>
        /// <param name="symbols">The number of possible values for this attribute.</param>
        /// 
        public DecisionVariable(string name, int symbols)
            : this(name, new IntRange(0, symbols - 1))
        {
        }

        /// <summary>
        ///   Creates a set of decision variables from a <see cref="Codification"/> codebook.
        /// </summary>
        /// 
        /// <param name="codebook">The codebook containing information about the variables.</param>
        /// <param name="columns">The columns to consider as decision variables.</param>
        /// 
        /// <returns>An array of <see cref="DecisionVariable"/> objects 
        /// initialized with the values from the codebook.</returns>
        /// 
        public static DecisionVariable[] FromCodebook(Codification codebook, params string[] columns)
        {
            DecisionVariable[] variables = new DecisionVariable[columns.Length];

            for (int i = 0; i < variables.Length; i++)
            {
                string name = columns[i];

                Codification.Options col;

                if (codebook.Columns.TryGetValue(name, out col))
                    variables[i] = new DecisionVariable(name, col.Symbols);
                else
                    variables[i] = new DecisionVariable(name, DecisionVariableKind.Continuous);
            }

            return variables;
        }

    }


    /// <summary>
    ///   Collection of decision attributes.
    /// </summary>
    /// 
    [Serializable]
    public class DecisionVariableCollection : ReadOnlyCollection<DecisionVariable>
    {
        /// <summary>
        ///   Initializes a new instance of the <see cref="DecisionVariableCollection"/> class.
        /// </summary>
        /// 
        /// <param name="list">The list to initialize the collection.</param>
        /// 
        public DecisionVariableCollection(IList<DecisionVariable> list)
            : base(list) { }
    }







    /// <summary>
    ///   Collection of decision nodes. A decision branch specifies the index of
    ///   an attribute whose current value should be compared against its children
    ///   nodes. The type of the comparison is specified in each child node.
    /// </summary>
    /// 
    //[Serializable]
    public class DecisionBranchNodeCollection : Collection<DecisionNode>
    {

        //[NonSerialized]
        private DecisionNode owner;

        /// <summary>
        ///   Gets or sets the index of the attribute to be
        ///   used in this stage of the decision process.
        /// </summary>
        /// 
        public int AttributeIndex { get; set; }

        /// <summary>
        ///   Gets the attribute that is being used in
        ///   this stage of the decision process, given
        ///   by the current <see cref="AttributeIndex"/>
        /// </summary>
        /// 
        public DecisionVariable Attribute
        {
            get
            {
                // TODO: Remove the obsolete attribute and make owner mandatory.
                if (owner == null)
                    return null;

                return owner.Owner.Attributes[AttributeIndex];
            }
        }

        /// <summary>
        ///   Gets or sets the decision node that contains this collection.
        /// </summary>
        /// 
        public DecisionNode Owner
        {
            get { return owner; }
            set { owner = value; }
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="DecisionBranchNodeCollection"/> class.
        /// </summary>
        /// 
        [Obsolete("Please specify an owner instead.")]
        public DecisionBranchNodeCollection() { }

        /// <summary>
        ///   Initializes a new instance of the <see cref="DecisionBranchNodeCollection"/> class.
        /// </summary>
        /// 
        /// <param name="owner">The <see cref="DecisionNode"/> to whom
        ///   this <see cref="DecisionBranchNodeCollection"/> belongs.</param>
        /// 
        public DecisionBranchNodeCollection(DecisionNode owner)
        {
            this.owner = owner;
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="DecisionBranchNodeCollection"/> class.
        /// </summary>
        /// 
        /// <param name="attributeIndex">Index of the attribute to be processed.</param>
        /// 
        /// <param name="nodes">The children nodes. Each child node should be
        /// responsible for a possible value of a discrete attribute, or for
        /// a region of a continuous-valued attribute.</param>
        /// 
        [Obsolete("Please specify an owner instead.")]
        public DecisionBranchNodeCollection(int attributeIndex, DecisionNode[] nodes)
            : base(nodes)
        {
            if (nodes == null)
                throw new ArgumentNullException("nodes");
            if (nodes.Length == 0)
                throw new ArgumentException("Node collection is empty.", "nodes");

            this.AttributeIndex = attributeIndex;
            this.owner = nodes[0].Parent;
        }

        /// <summary>
        ///   Adds the elements of the specified collection to the end of the collection.
        /// </summary>
        /// 
        /// <param name="children">The child nodes to be added.</param>
        /// 
        public void AddRange(IEnumerable<DecisionNode> children)
        {
            foreach (var node in children) Add(node);
        }
    }


















    /// <summary>
    ///   Decision Tree (DT) Node.
    /// </summary>
    /// 
    /// <remarks>
    ///   Each node of a decision tree can play two roles. When a node is not a leaf, it
    ///   contains a <see cref="DecisionBranchNodeCollection"/> with a collection of child nodes. The
    ///   branch specifies an attribute index, indicating which column from the data set
    ///   (the attribute) should be compared against its children values. The type of the
    ///   comparison is specified by each of the children. When a node is a leaf, it will
    ///   contain the output value which should be decided for when the node is reached.
    /// </remarks>
    /// 
    /// <seealso cref="DecisionTree"/>
    /// 
    //[Serializable]
    public class DecisionNode
    {

        //[NonSerialized]
        private DecisionTree owner;

        //[NonSerialized]
        private DecisionNode parent;


        /// <summary>
        ///   Gets or sets the value this node responds to
        ///   whenever this node acts as a child node. This
        ///   value is set only when the node has a parent.
        /// </summary>
        /// 
        public double? Value { get; set; }

        /// <summary>
        ///   Gets or sets the type of the comparison which
        ///   should be done against <see cref="Value"/>.
        /// </summary>
        /// 
        public ComparisonKind Comparison { get; set; }

        /// <summary>
        ///   If this is a leaf node, gets or sets the output
        ///   value to be decided when this node is reached.
        /// </summary>
        /// 
        public int? Output { get; set; }

        /// <summary>
        ///   If this is not a leaf node, gets or sets the collection
        ///   of child nodes for this node, together with the attribute
        ///   determining the reasoning process for those children.
        /// </summary>
        /// 
        public DecisionBranchNodeCollection Branches { get; set; }

        /// <summary>
        ///   Gets or sets the parent of this node. If this is a root
        ///   node, the parent is <c>null</c>.
        /// </summary>
        /// 
        public DecisionNode Parent
        {
            get { return parent; }
            set { parent = value; }
        }

        /// <summary>
        ///   Gets the <see cref="DecisionTree"/> containing this node.
        /// </summary>
        /// 
        public DecisionTree Owner
        {
            get { return owner; }
            set { owner = value; }
        }

        /// <summary>
        ///   Creates a new decision node.
        /// </summary>
        /// 
        /// <param name="owner">The owner tree for this node.</param>
        /// 
        public DecisionNode(DecisionTree owner)
        {
            Owner = owner;
            Comparison = ComparisonKind.None;
            Branches = new DecisionBranchNodeCollection(this);
        }

        /// <summary>
        ///   Gets a value indicating whether this instance is a root node (has no parent).
        /// </summary>
        /// 
        /// <value><c>true</c> if this instance is a root; otherwise, <c>false</c>.</value>
        /// 
        public bool IsRoot
        {
            get { return Parent == null; }
        }

        /// <summary>
        ///   Gets a value indicating whether this instance is a leaf (has no children).
        /// </summary>
        /// 
        /// <value><c>true</c> if this instance is a leaf; otherwise, <c>false</c>.</value>
        /// 
        public bool IsLeaf
        {
            get { return Branches == null || Branches.Count == 0; }
        }



        /// <summary>
        ///   Computes whether a value satisfies
        ///   the condition imposed by this node.
        /// </summary>
        /// 
        /// <param name="x">The value x.</param>
        /// 
        /// <returns><c>true</c> if the value satisfies this node's
        /// condition; otherwise, <c>false</c>.</returns>
        /// 
        public bool Compute(double x)
        {
            switch (Comparison)
            {
                case ComparisonKind.Equal:
                    return (x == Value);

                case ComparisonKind.GreaterThan:
                    return (x > Value);

                case ComparisonKind.GreaterThanOrEqual:
                    return (x >= Value);

                case ComparisonKind.LessThan:
                    return (x < Value);

                case ComparisonKind.LessThanOrEqual:
                    return (x <= Value);

                case ComparisonKind.NotEqual:
                    return (x != Value);

                default:
                    throw new InvalidOperationException();
            }
        }


        /// <summary>
        ///   Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// 
        /// <returns>
        ///   A <see cref="System.String"/> that represents this instance.
        /// </returns>
        /// 
		/*
        public override string ToString()
        {
            return toString(null);
        }

        /// <summary>
        ///   Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// 
        /// <returns>
        ///   A <see cref="System.String"/> that represents this instance.
        /// </returns>
        /// 
        public string ToString(Codification codebook)
        {
            return toString(codebook);
        }


        private string toString(Codification codebook)
        {
            if (IsRoot)
                return "Root";

            String name = Owner.Attributes[Parent.Branches.AttributeIndex].Name;

            if (String.IsNullOrEmpty(name))
                name = "x" + Parent.Branches.AttributeIndex;

            String op = ComparisonExtensions.ToString(Comparison);

            String value;
            if (codebook != null && Value.HasValue && codebook.Columns.Contains(name))
                value = codebook.Translate(name, (int)Value.Value);

            else value = Value.ToString();


            return String.Format("{0} {1} {2}", name, op, value);
        }





        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if (Branches != null)
            {
                Branches.Owner = this;

                foreach (DecisionNode node in Branches)
                {
                    node.Parent = this;
                }
            }
        }
		*/
    }















	//DECISION TREE



    /// <summary>
    ///   Decision tree.
    /// </summary>
    /// 
    /// <remarks>
    /// <para>
    ///   Represent a decision tree which can be compiled to
    ///   code at run-time. For sample usage and example of
    ///   learning, please see the <see cref="Learning.ID3Learning">
    ///   ID3 learning algorithm for decision trees</see>.</para>
    /// </remarks>
    ///
    /// <seealso cref="Learning.ID3Learning"/>
    /// <seealso cref="Learning.C45Learning"/>
    ///
    //[Serializable]
    public class DecisionTree : IEnumerable<DecisionNode>
    {
        /// <summary>
        ///   Gets or sets the root node for this tree.
        /// </summary>
        /// 
        public DecisionNode Root { get; set; }

        /// <summary>
        ///   Gets the collection of attributes processed by this tree.
        /// </summary>
        /// 
        public DecisionVariableCollection Attributes { get; private set; }

        /// <summary>
        ///   Gets the number of distinct output
        ///   classes classified by this tree.
        /// </summary>
        /// 
        public int OutputClasses { get; private set; }

        /// <summary>
        ///   Gets the number of input attributes
        ///   expected by this tree.
        /// </summary>
        /// 
        public int InputCount { get; private set; }

        /// <summary>
        ///   Creates a new <see cref="DecisionTree"/> to process
        ///   the given <paramref name="attributes"/> and the given
        ///   number of possible <paramref name="outputClasses"/>.
        /// </summary>
        /// 
        /// <param name="attributes">An array specifying the attributes to be processed by this tree.</param>
        /// <param name="outputClasses">The number of possible output classes for the given attributes.</param>
        /// 
        public DecisionTree(IList<DecisionVariable> attributes, int outputClasses)
        {
            if (outputClasses <= 0)
                throw new ArgumentOutOfRangeException("outputClasses");

            if (attributes == null)
                throw new ArgumentNullException("attributes");

            for (int i = 0; i < attributes.Count; i++)
                if (attributes[i].Range.Length == 0)
                    throw new ArgumentException("Attribute " + i + " is a constant.");


            this.Attributes = new DecisionVariableCollection(attributes);
            this.InputCount = attributes.Count;
            this.OutputClasses = outputClasses;
        }


        /// <summary>
        ///   Computes the decision for a given input.
        /// </summary>
        /// 
        /// <param name="input">The input data.</param>
        /// 
        /// <returns>A predicted class for the given input.</returns>
        /// 
        public int Compute(int[] input)
        {
            double[] x = new double[input.Length];
            for (int i = 0; i < input.Length; i++)
                x[i] = input[i];

            return Compute(x);
        }

        /// <summary>
        ///   Computes the tree decision for a given input.
        /// </summary>
        /// 
        /// <param name="input">The input data.</param>
        /// 
        /// <returns>A predicted class for the given input.</returns>
        /// 
        public int Compute(double[] input)
        {
            if (Root == null)
                throw new InvalidOperationException();

            return Compute(input, Root);
        }

        /// <summary>
        ///   Computes the tree decision for a given input.
        /// </summary>
        /// 
        /// <param name="input">The input data.</param>
        /// <param name="subtree">The node where the decision starts.</param>
        /// 
        /// <returns>A predicted class for the given input.</returns>
        /// 
        public int Compute(double[] input, DecisionNode subtree)
        {
            if (subtree == null) 
                throw new ArgumentNullException("subtree");

            if (subtree.Owner != this) 
                throw new ArgumentException("The node does not belong to this tree.", "subtree");

            DecisionNode current = subtree;

            // Start reasoning
            while (current != null)
            {
                // Check if this is a leaf
                if (current.IsLeaf)
                {
                    // This is a leaf node. The decision
                    // process thus should stop here.

                    return (current.Output.HasValue) ? current.Output.Value : -1;
                }

                // This node is not a leaf. Continue the
                // decision process following the children

                // Get the next attribute to guide reasoning
                int attribute = current.Branches.AttributeIndex;

                // Check which child is responsible for dealing
                // which the particular value of the attribute
                DecisionNode nextNode = null;

                foreach (DecisionNode branch in current.Branches)
                {
                    if (branch.Compute(input[attribute]))
                    {
                        // This is the child node responsible for dealing
                        // which this particular attribute value. Choose it
                        // to continue reasoning.

                        nextNode = branch; break;
                    }
                }

                current = nextNode;
            }

            // Normal execution should not reach here.
            throw new InvalidOperationException("The tree is degenerated. This is often a sign that "
                + "the tree is expecting discrete inputs, but it was given only real values.");
        }



        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            foreach (DecisionNode node in this)
            {
                node.Owner = this;
            }
        }


        /// <summary>
        ///   Returns an enumerator that iterates through the tree.
        /// </summary>
        /// 
        /// <returns>
        ///   An <see cref="T:System.Collections.IEnumerator"/> object that can be 
        ///   used to iterate through the collection.
        /// </returns>
        /// 
        public IEnumerator<DecisionNode> GetEnumerator()
        {
            if (Root == null)
                yield break;

            var stack = new Stack<DecisionNode>(new[] { Root });

            while (stack.Count != 0)
            {
                DecisionNode current = stack.Pop();

                yield return current;

                if (current.Branches != null)
                    for (int i = current.Branches.Count - 1; i >= 0; i--)
                        stack.Push(current.Branches[i]);
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        ///   Traverse the tree using a <see cref="DecisionTreeTraversal">tree 
        ///   traversal method</see>. Can be iterated with a foreach loop.
        /// </summary>
        /// 
        /// <param name="method">The tree traversal method. Common methods are
        /// available in the <see cref="TreeTraversal"/>static class.</param>
        /// 
        /// <returns>An <see cref="IEnumerable{T}"/> object which can be used to
        /// traverse the tree using the chosen traversal method.</returns>
        /// 
        public IEnumerable<DecisionNode> Traverse(DecisionTreeTraversalMethod method)
        {
            return new TreeTraversal(Root, method);
        }

        /// <summary>
        ///   Traverse a subtree using a <see cref="DecisionTreeTraversal">tree 
        ///   traversal method</see>. Can be iterated with a foreach loop.
        /// </summary>
        /// 
        /// <param name="method">The tree traversal method. Common methods are
        /// available in the <see cref="TreeTraversal"/>static class.</param>
        /// <param name="subtree">The root of the subtree to be traversed.</param>
        /// 
        /// <returns>An <see cref="IEnumerable{T}"/> object which can be used to
        /// traverse the tree using the chosen traversal method.</returns>
        /// 
        public IEnumerable<DecisionNode> Traverse(DecisionTreeTraversalMethod method, DecisionNode subtree)
        {
            if (subtree.Owner != this) throw new ArgumentException(
                "The node does not belong to this tree.", "subtree");
            return new TreeTraversal(subtree, method);
        }

        /// <summary>
        ///   Transforms the tree into a set of <see cref="DecisionSet">decision rules</see>.
        /// </summary>
        /// 
        /// <returns>A <see cref="DecisionSet"/> created from this tree.</returns>
        /// 
        public DecisionSet ToRules()
        {
            return DecisionSet.FromDecisionTree(this);
        }

#if !NET35
        /// <summary>
        ///   Creates an <see cref="Expression">Expression Tree</see> representation
        ///   of this decision tree, which can in turn be compiled into code.
        /// </summary>
        /// 
        /// <returns>A tree in the form of an expression tree.</returns>
        /// 
        public Expression<Func<double[], int>> ToExpression()
        {
            DecisionTreeExpressionCreator compiler = new DecisionTreeExpressionCreator(this);
            return compiler.Create();
        }

        /// <summary>
        ///   Creates a .NET assembly (.dll) containing a static class of
        ///   the given name implementing the decision tree. The class will
        ///   contain a single static Compute method implementing the tree.
        /// </summary>
        /// 
        /// <param name="assemblyName">The name of the assembly to generate.</param>
        /// <param name="className">The name of the generated static class.</param>
        /// 
        public void ToAssembly(string assemblyName, string className)
        {
            ToAssembly(assemblyName, "Accord.MachineLearning.DecisionTrees.Custom", className);
        }

        /// <summary>
        ///   Creates a .NET assembly (.dll) containing a static class of
        ///   the given name implementing the decision tree. The class will
        ///   contain a single static Compute method implementing the tree.
        /// </summary>
        /// 
        /// <param name="assemblyName">The name of the assembly to generate.</param>
        /// <param name="moduleName">The namespace which should contain the class.</param>
        /// <param name="className">The name of the generated static class.</param>
        /// 
        public void ToAssembly(string assemblyName, string moduleName, string className)
        {
            AssemblyBuilder da = AppDomain.CurrentDomain.DefineDynamicAssembly(
                new AssemblyName(assemblyName), AssemblyBuilderAccess.Save);

            ModuleBuilder dm = da.DefineDynamicModule(moduleName, assemblyName);
            TypeBuilder dt = dm.DefineType(className);
            MethodBuilder method = dt.DefineMethod("Compute",
                MethodAttributes.Public | MethodAttributes.Static);

            // Compile the tree into a method
            ToExpression().CompileToMethod(method);

            dt.CreateType();
            da.Save(assemblyName);
        }
#endif


     

        /// <summary>
        ///   Generates a C# class implementing the decision tree.
        /// </summary>
        /// 
        /// <param name="className">The name for the generated class.</param>
        /// 
        /// <returns>A string containing the generated class.</returns>
        /// 
        public string ToCode(string className)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                TextWriter writer = new StreamWriter(stream);
                var treeWriter = new DecisionTreeWriter(writer);
                treeWriter.Write(this, className);
                writer.Flush();

                stream.Seek(0, SeekOrigin.Begin);
                TextReader reader = new StreamReader(stream);
                return reader.ReadToEnd();
            }
        }

        /// <summary>
        ///   Generates a C# class implementing the decision tree.
        /// </summary>
        /// 
        /// <param name="className">The name for the generated class.</param>
        /// <param name="writer">The <see cref="TextWriter"/> where the class should be written.</param>
        /// 
        public void ToCode(TextWriter writer, string className)
        {
            var treeWriter = new DecisionTreeWriter(writer);
            treeWriter.Write(this, className);
        }


        /// <summary>
        ///   Loads a tree from a file.
        /// </summary>
        /// 
        /// <param name="path">The path to the file from which the tree is to be deserialized.</param>
        /// 
        /// <returns>The deserialized tree.</returns>
        /// 
        public void Save(string path)
        {
            using (FileStream fs = new FileStream(path, FileMode.Create))
            {
                Save(fs);
            }
        }

        /// <summary>
        ///   Saves the tree to a stream.
        /// </summary>
        /// 
        /// <param name="stream">The stream to which the tree is to be serialized.</param>
        /// 
        public void Save(Stream stream)
        {
            BinaryFormatter b = new BinaryFormatter();
            b.Serialize(stream, this);
        }

        /// <summary>
        ///   Loads a tree from a stream.
        /// </summary>
        /// 
        /// <param name="stream">The stream from which the tree is to be deserialized.</param>
        /// 
        /// <returns>The deserialized tree.</returns>
        /// 
        public static DecisionTree Load(Stream stream)
        {
            BinaryFormatter b = new BinaryFormatter();
            return (DecisionTree)b.Deserialize(stream);
        }

        /// <summary>
        ///   Loads a tree from a file.
        /// </summary>
        /// 
        /// <param name="path">The path to the tree from which the machine is to be deserialized.</param>
        /// 
        /// <returns>The deserialized tree.</returns>
        /// 
        public static DecisionTree Load(string path)
        {
            using (FileStream fs = new FileStream(path, FileMode.Open))
            {
                return Load(fs);
            }
        }



        private class TreeTraversal : IEnumerable<DecisionNode>
        {
            private DecisionNode tree;
            private DecisionTreeTraversalMethod method;

            public TreeTraversal(DecisionNode tree, DecisionTreeTraversalMethod method)
            {
                this.tree = tree;
                this.method = method;
            }

            public IEnumerator<DecisionNode> GetEnumerator()
            {
                return method(tree);
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return method(tree);
            }
        }
    }






    /// <summary>
    ///   C4.5 Learning algorithm for <see cref="DecisionTree">Decision Trees</see>.
    /// </summary>
    /// 
    /// <remarks>
    /// <para>
    ///   References:
    ///   <list type="bullet">
    ///     <item><description>
    ///       Quinlan, J. R. C4.5: Programs for Machine Learning. Morgan
    ///       Kaufmann Publishers, 1993.</description></item>
    ///     <item><description>
    ///       Quinlan, J. R. C4.5: Programs for Machine Learning. Morgan
    ///       Kaufmann Publishers, 1993.</description></item>
    ///     <item><description>
    ///       Quinlan, J. R. Improved use of continuous attributes in c4.5. Journal
    ///       of Artificial Intelligence Research, 4:77-90, 1996.</description></item>
    ///     <item><description>
    ///       Mitchell, T. M. Machine Learning. McGraw-Hill, 1997. pp. 55-58. </description></item>
    ///     <item><description><a href="http://en.wikipedia.org/wiki/ID3_algorithm">
    ///       Wikipedia, the free encyclopedia. ID3 algorithm. Available on 
    ///       http://en.wikipedia.org/wiki/ID3_algorithm </a></description></item>
    ///   </list>
    /// </para>   
    /// </remarks>
    ///
    /// <see cref="ID3Learning"/>
    /// 
    /// <example>
    /// <code>
    /// // This example uses the Nursery Database available from the University of
    /// // California Irvine repository of machine learning databases, available at
    /// //
    /// //   http://archive.ics.uci.edu/ml/machine-learning-databases/nursery/nursery.names
    /// //
    /// // The description paragraph is listed as follows.
    /// //
    /// //   Nursery Database was derived from a hierarchical decision model
    /// //   originally developed to rank applications for nursery schools. It
    /// //   was used during several years in 1980's when there was excessive
    /// //   enrollment to these schools in Ljubljana, Slovenia, and the
    /// //   rejected applications frequently needed an objective
    /// //   explanation. The final decision depended on three subproblems:
    /// //   occupation of parents and child's nursery, family structure and
    /// //   financial standing, and social and health picture of the family.
    /// //   The model was developed within expert system shell for decision
    /// //   making DEX (M. Bohanec, V. Rajkovic: Expert system for decision
    /// //   making. Sistemica 1(1), pp. 145-157, 1990.).
    /// //
    /// 
    /// // Let's begin by loading the raw data. This string variable contains
    /// // the contents of the nursery.data file as a single, continuous text.
    /// //
    /// string nurseryData = Resources.nursery;
    /// 
    /// // Those are the input columns available in the data
    /// //
    /// string[] inputColumns = 
    /// {
    ///     "parents", "has_nurs", "form", "children",
    ///     "housing", "finance", "social", "health"
    /// };
    /// 
    /// // And this is the output, the last column of the data.
    /// //
    /// string outputColumn = "output";
    ///             
    /// 
    /// // Let's populate a data table with this information.
    /// //
    /// DataTable table = new DataTable("Nursery");
    /// table.Columns.Add(inputColumns);
    /// table.Columns.Add(outputColumn);
    /// 
    /// string[] lines = nurseryData.Split(
    ///     new[] { Environment.NewLine }, StringSplitOptions.None);
    /// 
    /// foreach (var line in lines)
    ///     table.Rows.Add(line.Split(','));
    /// 
    /// 
    /// // Now, we have to convert the textual, categorical data found
    /// // in the table to a more manageable discrete representation.
    /// //
    /// // For this, we will create a codebook to translate text to
    /// // discrete integer symbols:
    /// //
    /// Codification codebook = new Codification(table);
    /// 
    /// // And then convert all data into symbols
    /// //
    /// DataTable symbols = codebook.Apply(table);
    /// double[][] inputs = symbols.ToArray(inputColumns);
    /// int[] outputs = symbols.ToArray&lt;int>(outputColumn);
    /// 
    /// // From now on, we can start creating the decision tree.
    /// //
    /// var attributes = DecisionVariable.FromCodebook(codebook, inputColumns);
    /// DecisionTree tree = new DecisionTree(attributes, outputClasses: 5);
    /// 
    /// 
    /// // Now, let's create the C4.5 algorithm
    /// C45Learning c45 = new C45Learning(tree);
    /// 
    /// // and learn a decision tree. The value of
    /// //   the error variable below should be 0.
    /// //
    /// double error = c45.Run(inputs, outputs);
    /// 
    /// 
    /// // To compute a decision for one of the input points,
    /// //   such as the 25-th example in the set, we can use
    /// //
    /// int y = tree.Compute(inputs[25]);
    /// 
    /// // Finally, we can also convert our tree to a native
    /// // function, improving efficiency considerably, with
    /// //
    /// Func&lt;double[], int> func = tree.ToExpression().Compile();
    /// 
    /// // Again, to compute a new decision, we can just use
    /// //
    /// int z = func(inputs[25]);
    /// </code>
    /// </example>
    ///
    //[Serializable]
    public class C45Learning
    {


        private DecisionTree tree;

        private int maxHeight;
        private int splitStep;

        private double[][] thresholds;
        private IntRange[] inputRanges;
        private int outputClasses;

        private bool[] attributes;


        /// <summary>
        ///   Gets or sets the maximum allowed 
        ///   height when learning a tree.
        /// </summary>
        /// 
        public int MaxHeight
        {
            get { return maxHeight; }
            set
            {
                if (maxHeight <= 0 || maxHeight > attributes.Length)
                    throw new ArgumentOutOfRangeException("value",
                        "The height must be greater than zero and less than the number of variables in the tree.");
                maxHeight = value;
            }
        }

        /// <summary>
        ///   Gets or sets the step at which the samples will
        ///   be divided when dividing continuous columns in
        ///   binary classes. Default is 1.
        /// </summary>
        /// 
        public int SplitStep
        {
            get { return splitStep; }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException("value",
                        "The split step must be greater than zero.");
                splitStep = value;
            }
        }

        /// <summary>
        ///   Creates a new C4.5 learning algorithm.
        /// </summary>
        /// 
        /// <param name="tree">The decision tree to be generated.</param>
        /// 
        public C45Learning(DecisionTree tree)
        {
            // Initial argument checking
            if (tree == null)
                throw new ArgumentNullException("tree");

            this.tree = tree;
            this.attributes = new bool[tree.InputCount];
            this.inputRanges = new IntRange[tree.InputCount];
            this.outputClasses = tree.OutputClasses;
            this.maxHeight = attributes.Length;
            this.splitStep = 1;

            for (int i = 0; i < inputRanges.Length; i++)
                inputRanges[i] = tree.Attributes[i].Range.ToIntRange(false);
        }

        /// <summary>
        ///   Runs the learning algorithm, creating a decision
        ///   tree modeling the given inputs and outputs.
        /// </summary>
        /// 
        /// <param name="inputs">The inputs.</param>
        /// <param name="outputs">The corresponding outputs.</param>
        /// 
        /// <returns>The error of the generated tree.</returns>
        /// 
        public double Run(double[][] inputs, int[] outputs)
        {
            // Initial argument check
            checkArgs(inputs, outputs);

            for (int i = 0; i < attributes.Length; i++)
                attributes[i] = false;

            thresholds = new double[tree.Attributes.Count][];

            List<double> candidates = new List<double>(inputs.Length);

            // 0. Create candidate split thresholds for each attribute
            for (int i = 0; i < tree.Attributes.Count; i++)
            {
                if (tree.Attributes[i].Nature == DecisionVariableKind.Continuous)
                {
                    double[] v = inputs.GetColumn(i);
                    int[] o = (int[])outputs.Clone();

                    Array.Sort(v, o);

                    for (int j = 0; j < v.Length - 1; j++)
                    {
                        // Add as candidate thresholds only adjacent values v[i] and v[i+1]
                        // belonging to different classes, following the results by Fayyad
                        // and Irani (1992). See footnote on Quinlan (1996).

                        if (o[j] != o[j + 1])
                            candidates.Add((v[j] + v[j + 1]) / 2.0);
                    }


                    thresholds[i] = candidates.ToArray();
                    candidates.Clear();
                }
            }


            // 1. Create a root node for the tree
            tree.Root = new DecisionNode(tree);

            split(tree.Root, inputs, outputs);

            return ComputeError(inputs, outputs);
        }

        /// <summary>
        ///   Computes the prediction error for the tree
        ///   over a given set of input and outputs.
        /// </summary>
        /// 
        /// <param name="inputs">The input points.</param>
        /// <param name="outputs">The corresponding output labels.</param>
        /// 
        /// <returns>The percentage error of the prediction.</returns>
        /// 
        public double ComputeError(double[][] inputs, int[] outputs)
        {
            int miss = 0;
            for (int i = 0; i < inputs.Length; i++)
            {
                if (tree.Compute(inputs[i]) != outputs[i])
                    miss++;
            }

            return (double)miss / inputs.Length;
        }

        private void split(DecisionNode root, double[][] input, int[] output)
        {

            // 2. If all examples are for the same class, return the single-node
            //    tree with the output label corresponding to this common class.
            double entropy = Statistics.Tools.Entropy(output, outputClasses);

            if (entropy == 0)
            {
                if (output.Length > 0)
                    root.Output = output[0];
                return;
            }

            // 3. If number of predicting attributes is empty, then return the single-node
            //    tree with the output label corresponding to the most common value of
            //    the target attributes in the examples.
            int predictors = attributes.Count(x => x == false);

            if (predictors <= attributes.Length - maxHeight)
            {
                root.Output = Statistics.Tools.Mode(output);
                return;
            }


            // 4. Otherwise, try to select the attribute which
            //    best explains the data sample subset.

            double[] scores = new double[predictors];
            double[] entropies = new double[predictors];
            double[] thresholds = new double[predictors];
            int[][][] partitions = new int[predictors][][];

            // Retrieve candidate attribute indices
            int[] candidates = new int[predictors];
            for (int i = 0, k = 0; i < attributes.Length; i++)
                if (!attributes[i]) candidates[k++] = i;


            // For each attribute in the data set
#if SERIAL
            for (int i = 0; i < scores.Length; i++)
#else
            Parallel.For(0, scores.Length, i =>
#endif
            {
                scores[i] = computeGainRatio(input, output, candidates[i],
                    entropy, out partitions[i], out thresholds[i]);
            }
#if !SERIAL
);
#endif

            // Select the attribute with maximum gain ratio
            int maxGainIndex; scores.Max(out maxGainIndex);
            var maxGainPartition = partitions[maxGainIndex];
            var maxGainEntropy = entropies[maxGainIndex];
            var maxGainAttribute = candidates[maxGainIndex];
            var maxGainRange = inputRanges[maxGainAttribute];
            var maxGainThreshold = thresholds[maxGainIndex];

            // Mark this attribute as already used
            attributes[maxGainAttribute] = true;

            double[][] inputSubset;
            int[] outputSubset;

            // Now, create next nodes and pass those partitions as their responsibilities. 
            if (tree.Attributes[maxGainAttribute].Nature == DecisionVariableKind.Discrete)
            {
                // This is a discrete nature attribute. We will branch at each
                // possible value for the discrete variable and call recursion.
                DecisionNode[] children = new DecisionNode[maxGainPartition.Length];

                // Create a branch for each possible value
                for (int i = 0; i < children.Length; i++)
                {
                    children[i] = new DecisionNode(tree)
                    {
                        Parent = root,
                        Value = i + maxGainRange.Min,
                        Comparison = ComparisonKind.Equal,
                    };

                    inputSubset = input.Submatrix(maxGainPartition[i]);
                    outputSubset = output.Submatrix(maxGainPartition[i]);
                    split(children[i], inputSubset, outputSubset); // recursion
                }

                root.Branches.AttributeIndex = maxGainAttribute;
                root.Branches.AddRange(children);
            }

            else if (maxGainPartition.Length > 1)
            {
                // This is a continuous nature attribute, and we achieved two partitions
                // using the partitioning scheme. We will branch on two possible settings:
                // either the value is greater than a currently detected optimal threshold 
                // or it is less.

                DecisionNode[] children = 
                {
                    new DecisionNode(tree) 
                    {
                        Parent = root, Value = maxGainThreshold,
                        Comparison = ComparisonKind.LessThanOrEqual 
                    },

                    new DecisionNode(tree)
                    {
                        Parent = root, Value = maxGainThreshold,
                        Comparison = ComparisonKind.GreaterThan
                    }
                };

                // Create a branch for lower values
                inputSubset = input.Submatrix(maxGainPartition[0]);
                outputSubset = output.Submatrix(maxGainPartition[0]);
                split(children[0], inputSubset, outputSubset);

                // Create a branch for higher values
                inputSubset = input.Submatrix(maxGainPartition[1]);
                outputSubset = output.Submatrix(maxGainPartition[1]);
                split(children[1], inputSubset, outputSubset);

                root.Branches.AttributeIndex = maxGainAttribute;
                root.Branches.AddRange(children);
            }
            else
            {
                // This is a continuous nature attribute, but all variables are equal
                // to a constant. If there is only a constant value as the predictor 
                // and there are multiple output labels associated with this constant
                // value, there isn't much we can do. This node will be a leaf.

                // We will set the class label for this node as the
                // majority of the currently selected output classes.

                outputSubset = output.Submatrix(maxGainPartition[0]);
                root.Output = Statistics.Tools.Mode(outputSubset);
            }

            attributes[maxGainAttribute] = false;
        }


        private double computeGainRatio(double[][] input, int[] output, int attributeIndex,
            double entropy, out int[][] partitions, out double threshold)
        {
            double infoGain = computeInfoGain(input, output, attributeIndex, entropy, out partitions, out threshold);
            double splitInfo = Measures.SplitInformation(output.Length, partitions);

            return infoGain == 0 || splitInfo == 0 ? 0 : infoGain / splitInfo;
        }

        private double computeInfoGain(double[][] input, int[] output, int attributeIndex,
            double entropy, out int[][] partitions, out double threshold)
        {
            threshold = 0;

            if (tree.Attributes[attributeIndex].Nature == DecisionVariableKind.Discrete)
                return entropy - computeInfoDiscrete(input, output, attributeIndex, out partitions);

            return entropy + computeInfoContinuous(input, output, attributeIndex, out partitions, out threshold);
        }

        private double computeInfoDiscrete(double[][] input, int[] output,
            int attributeIndex, out int[][] partitions)
        {
            // Compute the information gain obtained by using
            // this current attribute as the next decision node.
            double info = 0;

            IntRange valueRange = inputRanges[attributeIndex];
            partitions = new int[valueRange.Length + 1][];


            // For each possible value of the attribute
            for (int i = 0; i < partitions.Length; i++)
            {
                int value = valueRange.Min + i;

                // Partition the remaining data set
                // according to the attribute values
                partitions[i] = input.Find(x => x[attributeIndex] == value);

                // For each of the instances under responsibility
                // of this node, check which have the same value
                int[] outputSubset = output.Submatrix(partitions[i]);

                // Check the entropy gain originating from this partitioning
                double e = Statistics.Tools.Entropy(outputSubset, outputClasses);

                info += ((double)outputSubset.Length / output.Length) * e;
            }

            return info;
        }

        private double computeInfoContinuous(double[][] input, int[] output,
            int attributeIndex, out int[][] partitions, out double threshold)
        {
            // Compute the information gain obtained by using
            // this current attribute as the next decision node.
            double[] t = thresholds[attributeIndex];

            double bestGain = Double.NegativeInfinity;
            double bestThreshold = t[0];
            partitions = null;

            List<int> idx1 = new List<int>(input.Length);
            List<int> idx2 = new List<int>(input.Length);

            List<int> output1 = new List<int>(input.Length);
            List<int> output2 = new List<int>(input.Length);

            double[] values = new double[input.Length];
            for (int i = 0; i < values.Length; i++)
                values[i] = input[i][attributeIndex];

            // For each possible splitting point of the attribute
            for (int i = 0; i < t.Length; i += splitStep)
            {
                // Partition the remaining data set
                // according to the threshold value
                double value = t[i];

                idx1.Clear();
                idx2.Clear();

                output1.Clear();
                output2.Clear();

                for (int j = 0; j < values.Length; j++)
                {
                    double x = values[j];

                    if (x <= value)
                    {
                        idx1.Add(j);
                        output1.Add(output[j]);
                    }
                    else if (x > value)
                    {
                        idx2.Add(j);
                        output2.Add(output[j]);
                    }
                }

                double p1 = (double)output1.Count / output.Length;
                double p2 = (double)output2.Count / output.Length;

                double splitGain =
                    -p1 * Statistics.Tools.Entropy(output1, outputClasses) +
                    -p2 * Statistics.Tools.Entropy(output2, outputClasses);

                if (splitGain > bestGain)
                {
                    bestThreshold = value;
                    bestGain = splitGain;

                    if (idx1.Count > 0 && idx2.Count > 0)
                        partitions = new int[][] { idx1.ToArray(), idx2.ToArray() };
                    else if (idx1.Count > 0)
                        partitions = new int[][] { idx1.ToArray() };
                    else if (idx2.Count > 0)
                        partitions = new int[][] { idx2.ToArray() };
                    else
                        partitions = new int[][] { };
                }
            }

            threshold = bestThreshold;
            return bestGain;
        }


        private void checkArgs(double[][] inputs, int[] outputs)
        {
            if (inputs == null)
                throw new ArgumentNullException("inputs");

            if (outputs == null)
                throw new ArgumentNullException("outputs");

            if (inputs.Length != outputs.Length)
                throw new DimensionMismatchException("outputs",
                    "The number of input vectors and output labels does not match.");

            if (inputs.Length == 0)
                throw new ArgumentOutOfRangeException("inputs",
                    "Training algorithm needs at least one training vector.");

            for (int i = 0; i < inputs.Length; i++)
            {
                if (inputs[i].Length != tree.InputCount)
                {
                    throw new DimensionMismatchException("inputs", "The size of the input vector at index "
                        + i + " does not match the expected number of inputs of the tree."
                        + " All input vectors for this tree must have length " + tree.InputCount);
                }

                for (int j = 0; j < inputs[i].Length; j++)
                {
                    if (tree.Attributes[j].Nature != DecisionVariableKind.Discrete)
                        continue;

                    int min = (int)tree.Attributes[j].Range.Min;
                    int max = (int)tree.Attributes[j].Range.Max;

                    if (inputs[i][j] < min || inputs[i][j] > max)
                    {
                        throw new ArgumentOutOfRangeException("inputs", "The input vector at position "
                            + i + " contains an invalid entry at column " + j +
                            ". The value must be between the bounds specified by the decision tree " +
                            "attribute variables.");
                    }
                }
            }

            for (int i = 0; i < outputs.Length; i++)
            {
                if (outputs[i] < 0 || outputs[i] >= tree.OutputClasses)
                {
                    throw new ArgumentOutOfRangeException("outputs",
                      "The output label at index " + i + " should be equal to or higher than zero," +
                      "and should be lesser than the number of output classes expected by the tree.");
                }
            }
        }
    }
}

#endif