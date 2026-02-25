namespace PositionReporting.Api.Configuration;

public class Saml2Settings
{
    public string ServiceProviderEntityId { get; set; } = string.Empty;
    public string ServiceProviderReturnUrl { get; set; } = string.Empty;
    public string IdentityProviderEntityId { get; set; } = string.Empty;
    public string IdentityProviderMetadataUrl { get; set; } = string.Empty;
}