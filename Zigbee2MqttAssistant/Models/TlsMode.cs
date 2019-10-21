namespace Zigbee2MqttAssistant.Models
{
	public enum TlsMode
	{
		/// <summary>
		/// No TLS mode
		/// </summary>
		False,

		/// <summary>
		/// TLS mode activated
		/// </summary>
		True,

		/// <summary>
		/// TLS mode activated, but without any certificate check
		/// </summary>
		Insecure
	}
}
