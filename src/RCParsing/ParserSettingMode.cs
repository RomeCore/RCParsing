namespace RCParsing
{
	/// <summary>
	/// Defines how parser elements should handle specific settings propagation for local and children elements.
	/// </summary>
	public enum ParserSettingMode
	{
		/// <summary>
		/// Applies parent's setting for this element and all of its children. Ignores the local and global settings. The default mode.
		/// </summary>
		InheritForSelfAndChildren = 0,

		/// <summary>
		/// Apllies the local setting (if any) for this element and all of its children. This is default behavior when providing a local setting.
		/// </summary>
		LocalForSelfAndChildren,

		/// <summary>
		/// Applies local setting for this element only. Propagates the parent's setting to all child elements.
		/// </summary>
		LocalForSelfOnly,

		/// <summary>
		/// Applies parent's setting for this element only. Propagates the local setting to all child elements.
		/// </summary>
		LocalForChildrenOnly,

		/// <summary>
		/// Applies global setting for this element and all of its children.
		/// </summary>
		GlobalForSelfAndChildren,

		/// <summary>
		/// Applies global setting for this element only. Propagates the parent's setting to all child elements.
		/// </summary>
		GlobalForSelfOnly,

		/// <summary>
		/// Applies parent's setting for this element only. Propagates the global setting to all child elements.
		/// </summary>
		GlobalForChildrenOnly,
	}
}