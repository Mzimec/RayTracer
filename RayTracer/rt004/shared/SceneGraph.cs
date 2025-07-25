using OpenTK.Mathematics;

namespace rt004.shared {
    /// <summary>
    /// Represents the hierarchical structure of the scene, including nodes, lights, objects, and camera.
    /// </summary>
    public class SceneGraph {
        /// <summary>
        /// Gets the root node of the scene graph, typically an <see cref="InnerNode"/>.
        /// </summary>
        public SceneNode Root { get; private set; }

        /// <summary>
        /// Lookup dictionary for fast access to nodes by name.
        /// </summary>
        private Dictionary<string, SceneNode> _nodeLookUp = new Dictionary<string, SceneNode>();

        /// <summary>
        /// Gets the dictionary of light sources in the scene, indexed by name.
        /// </summary>
        public Dictionary<string, LightSource> Lights { get; private set; } = new Dictionary<string, LightSource>();

        /// <summary>
        /// Gets the dictionary of scene objects, indexed by name.
        /// </summary>
        public Dictionary<string, SceneObject> Objects { get; private set; } = new Dictionary<string, SceneObject>();

        /// <summary>
        /// Gets the dictionary of directional lights, indexed by name.
        /// </summary>
        public Dictionary<string, DirectionalLight> DirectionalLights { get; private set; } = new Dictionary<string, DirectionalLight>();

        /// <summary>
        /// Gets the camera for the scene, or null if no camera is registered.
        /// </summary>
        public Camera? Camera { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SceneGraph"/> class with a root node of the given name.
        /// </summary>
        /// <param name="rootName">The name of the root node.</param>
        public SceneGraph(string rootName = "Root") {
            Root = new InnerNode(rootName);
            RegisterNode(Root);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SceneGraph"/> class with the specified root node.
        /// </summary>
        /// <param name="root">The root node of the scene graph.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="root"/> is null.</exception>
        public SceneGraph(SceneNode root) {
            if (root == null) throw new ArgumentNullException(nameof(root), "Root node cannot be null.");
            Root = root;
            RegisterNode(Root);
        }

        /// <summary>
        /// Determines whether the specified node is valid to add to the scene graph.
        /// </summary>
        /// <param name="node">The node to check.</param>
        /// <returns>True if the node can be added; otherwise, false.</returns>
        public bool IsNodeValidToAdd(SceneNode node) {
            if (node == null || string.IsNullOrEmpty(node.Name)) return false;
            if (_nodeLookUp.ContainsKey(node.Name)) return false;
            return true;
        }

        /// <summary>
        /// Determines whether a node with the specified name can be registered.
        /// </summary>
        /// <param name="name">The name to check.</param>
        /// <returns>True if the name is valid and not already used; otherwise, false.</returns>
        public bool IsNodeValidToRegister(string name) {
            if (string.IsNullOrEmpty(name)) return false;
            if (_nodeLookUp.ContainsKey(name)) return false;
            return true;
        }

        /// <summary>
        /// Registers a node in the scene graph and updates relevant dictionaries.
        /// </summary>
        /// <param name="node">The node to register.</param>
        /// <exception cref="ArgumentException">Thrown if the node is invalid or already registered.</exception>
        public void RegisterNode(SceneNode node) {
            if (!IsNodeValidToAdd(node)) throw new ArgumentException("Invalid node to register.");
            _nodeLookUp[node.Name] = node;
            if (node is LightSource light) Lights[light.Name] = light;
            else if (node is Camera camera && Camera == null) Camera = camera;
            else if (node is SceneObject obj) {
                Objects[obj.Name] = obj;
                if (obj.Material is not null && obj.Material.ScatterModel.IsEmissive) {
                    // Optionally register emitter as a light source here
                }
            }
        }

        /// <summary>
        /// Unregisters a node from the scene graph and updates relevant dictionaries.
        /// </summary>
        /// <param name="node">The node to unregister.</param>
        /// <exception cref="ArgumentException">Thrown if the node is invalid.</exception>
        public void UnregisterNode(SceneNode node) {
            if (node == null || string.IsNullOrEmpty(node.Name)) throw new ArgumentException("Invalid node to unregister.");
            if (!_nodeLookUp.ContainsKey(node.Name)) return;
            _nodeLookUp.Remove(node.Name);
            if (node is LightSource light) Lights.Remove(light.Name);
            else if (node is Camera camera && Camera == camera) Camera = null;
            else if (node is SceneObject obj) Objects.Remove(obj.Name);
        }
    }

    /// <summary>
    /// Represents a node in the scene graph, which can be a camera, light, or object.
    /// </summary>
    public abstract class SceneNode : ITransformable {
        /// <summary>
        /// Gets or sets the scene graph this node belongs to.
        /// </summary>
        public SceneGraph? SceneGraph { get; set; }

        /// <summary>
        /// Gets or sets the name of the node.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the parent node.
        /// </summary>
        public InnerNode? Parent { get; set; }

        /// <summary>
        /// Gets the transform of the node.
        /// </summary>
        public Transform Transform { get; private set; }

        /// <summary>
        /// Gets the local-to-world transformation matrix.
        /// </summary>
        public Matrix4 LocalToWorld { get; protected set; }

        /// <summary>
        /// Gets the world-to-local transformation matrix.
        /// </summary>
        public Matrix4 WorldToLocal { get; protected set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SceneNode"/> class.
        /// </summary>
        /// <param name="name">The name of the node.</param>
        /// <param name="transform">The transform of the node. If null, a default transform is used.</param>
        public SceneNode(string name, Transform? transform = null) {
            Name = name;
            Transform = transform ?? new Transform(Vector3.Zero, Quaternion.Identity, Vector3.One);
            Transform.OnTransformChnaged += UpdateTransform;
            UpdateTransform();
        }

        /// <summary>
        /// Updates the transformation matrices of the node.
        /// </summary>
        public virtual void UpdateTransform() {
            LocalToWorld = Transform.LocalToParent * (Parent?.LocalToWorld ?? Matrix4.Identity);
            WorldToLocal = Matrix4.Invert(LocalToWorld);
        }

        /// <summary>
        /// Registers the node with the specified scene graph.
        /// </summary>
        /// <param name="graph">The scene graph to register with.</param>
        public virtual void Register(SceneGraph graph) {
            graph.RegisterNode(this);
            SceneGraph = graph;
        }

        /// <summary>
        /// Unregisters the node from the specified scene graph.
        /// </summary>
        /// <param name="graph">The scene graph to unregister from.</param>
        public virtual void Unregister(SceneGraph graph) {
            graph.UnregisterNode(this);
            SceneGraph = null;
        }

        /// <summary>
        /// Determines whether the specified node is an ancestor of this node.
        /// </summary>
        /// <param name="node">The node to check.</param>
        /// <returns>True if the specified node is an ancestor; otherwise, false.</returns>
        protected bool IsAncestor(SceneNode node) {
            var current = this;
            while (current != null) {
                if (current == node) return true;
                current = current.Parent;
            }
            return false;
        }
    }

    /// <summary>
    /// Represents a node that can have children and manage material overrides.
    /// </summary>
    public class InnerNode : SceneNode {
        /// <summary>
        /// Gets the dictionary of child nodes, indexed by name.
        /// </summary>
        public Dictionary<string, SceneNode> Children { get; private set; } = new Dictionary<string, SceneNode>();

        private Material? _materialOverride;

        /// <summary>
        /// Gets the material assigned to this node, or inherited from the parent.
        /// </summary>
        public Material? Material { get; protected set; }

        /// <summary>
        /// Gets or sets the material override for this node. Setting this will update all children recursively.
        /// </summary>
        public Material? MaterialOverride {
            get => _materialOverride;
            set {
                _materialOverride = value;
                OnMaterialOverrideUpdated(value);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InnerNode"/> class.
        /// </summary>
        /// <param name="name">The name of the node.</param>
        /// <param name="transform">The transform of the node.</param>
        /// <param name="materialOverride">Optional material override for this node.</param>
        public InnerNode(string name, Transform? transform = null, Material? materialOverride = null)
            : base(name, transform) {
            this.MaterialOverride = materialOverride;
        }

        /// <summary>
        /// Adds a child node to this node.
        /// </summary>
        /// <param name="child">The child node to add.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="child"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if adding the child would create a cycle.</exception>
        public void AddChild(SceneNode child) {
            if (child == null) throw new ArgumentNullException(nameof(child), "Child node cannot be null.");
            if (IsAncestor(child)) throw new InvalidOperationException("Cannot add a node as a child of itself or its descendants.");
            if (child.Parent != null) child.Parent.RemoveChild(child);
            Children[child.Name] = child;
            child.UpdateTransform();
            if (SceneGraph != null) child.Register(SceneGraph);
        }

        /// <summary>
        /// Removes a child node from this node.
        /// </summary>
        /// <param name="child">The child node to remove.</param>
        public void RemoveChild(SceneNode child) {
            if (!Children.ContainsKey(child.Name)) return;
            Children.Remove(child.Name);
            child.Parent = null;
            if (SceneGraph != null) child.Unregister(SceneGraph);
        }

        /// <summary>
        /// Updates the transformation matrices of this node and all its children.
        /// </summary>
        public override void UpdateTransform() {
            base.UpdateTransform();
            foreach (var (key, child) in Children) child.UpdateTransform();
        }

        /// <summary>
        /// Updates the material for this node and all children recursively.
        /// </summary>
        /// <param name="material">The new material to assign, or null to inherit from parent.</param>
        private void OnMaterialOverrideUpdated(Material? material = null) {
            Material = material ?? Parent?.Material;
            foreach (var (key, child) in Children) {
                if (child is IHasMaterial matNode) matNode.UpdateMaterial(material);
            }
        }

        /// <summary>
        /// Updates the material for this node and all children recursively, unless a material override is set.
        /// </summary>
        /// <param name="material">The new material to assign, or null to inherit from parent.</param>
        public void UpdateMaterial(Material? material = null) {
            if (MaterialOverride is not null) return;
            Material = material ?? Parent?.Material;
            foreach (var (key, child) in Children) {
                if (child is IHasMaterial matNode) matNode.UpdateMaterial(material);
            }
        }

        /// <summary>
        /// Registers this node and all its children with the specified scene graph.
        /// </summary>
        /// <param name="graph">The scene graph to register with.</param>
        public override void Register(SceneGraph graph) {
            base.Register(graph);
            foreach (var (key, child) in Children) child.Register(graph);
        }

        /// <summary>
        /// Unregisters this node and all its children from the specified scene graph.
        /// </summary>
        /// <param name="graph">The scene graph to unregister from.</param>
        public override void Unregister(SceneGraph graph) {
            base.Unregister(graph);
            foreach (var (key, child) in Children) child.Unregister(graph);
        }
    }
}