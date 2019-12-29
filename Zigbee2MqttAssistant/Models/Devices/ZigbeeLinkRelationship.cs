namespace Zigbee2MqttAssistant.Models.Devices
{
	public enum ZigbeeLinkRelationship
	{
		/// <summary>
		/// The other device is a parent of this one
		/// </summary>
		Parent,

		/// <summary>
		/// The other device is a child of this one
		/// </summary>
		Child,

		/// <summary>
		/// THe other device is a child to this one
		/// </summary>
		Sibling,

		/// <summary>
		/// The other device is a former child to this one
		/// </summary>
		FormerChild,

		/// <summary>
		/// No special relationship defined
		/// </summary>
		Other
	}
}
