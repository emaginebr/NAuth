using NAuth.DTO.Converters;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NAuth.DTO.User
{
    public class UserUpdatedInfo
    {
        [JsonPropertyName("userId")]
        public long UserId { get; set; }
        [JsonPropertyName("slug")]
        public string? Slug { get; set; }
        [JsonPropertyName("imageUrl")]
        public string? ImageUrl { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("email")]
        public string Email { get; set; }
        [JsonPropertyName("isAdmin")]
        public bool IsAdmin { get; set; }
        [JsonPropertyName("birthDate")]
        [JsonConverter(typeof(NullableDateTimeConverter))]
        public DateTime? BirthDate { get; set; }
        [JsonPropertyName("idDocument")]
        public string? IdDocument { get; set; }
        [JsonPropertyName("pixKey")]
        public string? PixKey { get; set; }
        [JsonPropertyName("status")]
        public int Status { get; set; }
        [JsonPropertyName("roles")]
        public IList<RoleInfo> Roles { get; set; }
        [JsonPropertyName("phones")]
        public IList<UserPhoneInfo> Phones { get; set; }
        [JsonPropertyName("addresses")]
        public IList<UserAddressInfo> Addresses { get; set; }
    }
}
