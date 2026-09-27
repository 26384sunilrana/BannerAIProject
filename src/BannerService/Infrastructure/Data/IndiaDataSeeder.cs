namespace BannerService.Infrastructure.Data
{
    using Domain.Entities;
    using Microsoft.EntityFrameworkCore;

    public class IndiaDataSeeder
    {
        public static async Task SeedIndiaDataAsync(ApplicationDbContext context)
        {
            try
            {
                // Check if India already seeded
                var indiaExists = await context.Countries.FirstOrDefaultAsync(c => c.ISOCode == "IN");
                if (indiaExists != null)
                    return; // Already seeded

                // Seed India country
                var india = new Country
                {
                    ISOCode = "IN",
                    Name = "India",
                    RegionName = "South Asia",
                    PhoneCode = "+91",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                context.Countries.Add(india);
                await context.SaveChangesAsync();

                // Seed all Indian states
                var states = GetIndianStates();
                foreach (var state in states)
                {
                    context.States.Add(state);
                }
                await context.SaveChangesAsync();

                // Seed all districts
                var districts = GetIndianDistricts();
                foreach (var district in districts)
                {
                    context.Districts.Add(district);
                }
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error seeding India master data", ex);
            }
        }

        private static List<State> GetIndianStates()
        {
            return new List<State>
            {
                new() { CountryCode = "IN", Code = "AP", Name = "Andhra Pradesh", RegionType = "State", IsActive = true },
                new() { CountryCode = "IN", Code = "AR", Name = "Arunachal Pradesh", RegionType = "State", IsActive = true },
                new() { CountryCode = "IN", Code = "AS", Name = "Assam", RegionType = "State", IsActive = true },
                new() { CountryCode = "IN", Code = "BR", Name = "Bihar", RegionType = "State", IsActive = true },
                new() { CountryCode = "IN", Code = "CG", Name = "Chhattisgarh", RegionType = "State", IsActive = true },
                new() { CountryCode = "IN", Code = "GA", Name = "Goa", RegionType = "State", IsActive = true },
                new() { CountryCode = "IN", Code = "GJ", Name = "Gujarat", RegionType = "State", IsActive = true },
                new() { CountryCode = "IN", Code = "HR", Name = "Haryana", RegionType = "State", IsActive = true },
                new() { CountryCode = "IN", Code = "HP", Name = "Himachal Pradesh", RegionType = "State", IsActive = true },
                new() { CountryCode = "IN", Code = "JK", Name = "Jammu and Kashmir", RegionType = "Union Territory", IsActive = true },
                new() { CountryCode = "IN", Code = "JH", Name = "Jharkhand", RegionType = "State", IsActive = true },
                new() { CountryCode = "IN", Code = "KA", Name = "Karnataka", RegionType = "State", IsActive = true },
                new() { CountryCode = "IN", Code = "KL", Name = "Kerala", RegionType = "State", IsActive = true },
                new() { CountryCode = "IN", Code = "MP", Name = "Madhya Pradesh", RegionType = "State", IsActive = true },
                new() { CountryCode = "IN", Code = "MH", Name = "Maharashtra", RegionType = "State", IsActive = true },
                new() { CountryCode = "IN", Code = "MN", Name = "Manipur", RegionType = "State", IsActive = true },
                new() { CountryCode = "IN", Code = "ML", Name = "Meghalaya", RegionType = "State", IsActive = true },
                new() { CountryCode = "IN", Code = "MZ", Name = "Mizoram", RegionType = "State", IsActive = true },
                new() { CountryCode = "IN", Code = "NL", Name = "Nagaland", RegionType = "State", IsActive = true },
                new() { CountryCode = "IN", Code = "OR", Name = "Odisha", RegionType = "State", IsActive = true },
                new() { CountryCode = "IN", Code = "PB", Name = "Punjab", RegionType = "State", IsActive = true },
                new() { CountryCode = "IN", Code = "RJ", Name = "Rajasthan", RegionType = "State", IsActive = true },
                new() { CountryCode = "IN", Code = "SK", Name = "Sikkim", RegionType = "State", IsActive = true },
                new() { CountryCode = "IN", Code = "TN", Name = "Tamil Nadu", RegionType = "State", IsActive = true },
                new() { CountryCode = "IN", Code = "TG", Name = "Telangana", RegionType = "State", IsActive = true },
                new() { CountryCode = "IN", Code = "TR", Name = "Tripura", RegionType = "State", IsActive = true },
                new() { CountryCode = "IN", Code = "UP", Name = "Uttar Pradesh", RegionType = "State", IsActive = true },
                new() { CountryCode = "IN", Code = "UK", Name = "Uttarakhand", RegionType = "State", IsActive = true },
                new() { CountryCode = "IN", Code = "WB", Name = "West Bengal", RegionType = "State", IsActive = true },
                new() { CountryCode = "IN", Code = "DL", Name = "Delhi", RegionType = "Union Territory", IsActive = true },
                new() { CountryCode = "IN", Code = "LA", Name = "Ladakh", RegionType = "Union Territory", IsActive = true },
                new() { CountryCode = "IN", Code = "PY", Name = "Puducherry", RegionType = "Union Territory", IsActive = true },
                new() { CountryCode = "IN", Code = "CH", Name = "Chandigarh", RegionType = "Union Territory", IsActive = true },
                new() { CountryCode = "IN", Code = "AN", Name = "Andaman and Nicobar Islands", RegionType = "Union Territory", IsActive = true },
                new() { CountryCode = "IN", Code = "DD", Name = "Daman and Diu", RegionType = "Union Territory", IsActive = true },
                new() { CountryCode = "IN", Code = "DN", Name = "Dadra and Nagar Haveli", RegionType = "Union Territory", IsActive = true },
                new() { CountryCode = "IN", Code = "LD", Name = "Lakshadweep", RegionType = "Union Territory", IsActive = true }
            };
        }

        private static List<District> GetIndianDistricts()
        {
            var states = GetIndianStates();
            var districts = new List<District>();

            // Helper to get state ID
            int GetStateId(string stateCode)
            {
                var state = states.FirstOrDefault(s => s.Code == stateCode);
                // We'll set proper IDs after states are created
                // For now, returning a placeholder that will be corrected in actual implementation
                return states.IndexOf(state) + 1;
            }

            // Maharashtra Districts (28)
            var maharashtraId = GetStateId("MH");
            districts.AddRange(new[]
            {
                new District { StateId = maharashtraId, Code = "MM", Name = "Mumbai", RegionType = "District", IsActive = true },
                new District { StateId = maharashtraId, Code = "PN", Name = "Pune", RegionType = "District", IsActive = true },
                new District { StateId = maharashtraId, Code = "NG", Name = "Nagpur", RegionType = "District", IsActive = true },
                new District { StateId = maharashtraId, Code = "TN", Name = "Thane", RegionType = "District", IsActive = true },
                new District { StateId = maharashtraId, Code = "SN", Name = "Solapur", RegionType = "District", IsActive = true },
                new District { StateId = maharashtraId, Code = "ST", Name = "Satara", RegionType = "District", IsActive = true },
                new District { StateId = maharashtraId, Code = "SD", Name = "Sangli", RegionType = "District", IsActive = true },
                new District { StateId = maharashtraId, Code = "SC", Name = "Sangli", RegionType = "District", IsActive = true },
                new District { StateId = maharashtraId, Code = "SH", Name = "Sindhudurg", RegionType = "District", IsActive = true },
                new District { StateId = maharashtraId, Code = "RT", Name = "Ratnagiri", RegionType = "District", IsActive = true },
                new District { StateId = maharashtraId, Code = "RY", Name = "Raigad", RegionType = "District", IsActive = true },
                new District { StateId = maharashtraId, Code = "RL", Name = "Raichur", RegionType = "District", IsActive = true },
                new District { StateId = maharashtraId, Code = "PS", Name = "Palghar", RegionType = "District", IsActive = true },
                new District { StateId = maharashtraId, Code = "OP", Name = "Osmangabad", RegionType = "District", IsActive = true },
                new District { StateId = maharashtraId, Code = "NR", Name = "Nandurbar", RegionType = "District", IsActive = true },
                new District { StateId = maharashtraId, Code = "ND", Name = "Nanded", RegionType = "District", IsActive = true },
                new District { StateId = maharashtraId, Code = "NC", Name = "Nashik", RegionType = "District", IsActive = true },
                new District { StateId = maharashtraId, Code = "JL", Name = "Jalgaon", RegionType = "District", IsActive = true },
                new District { StateId = maharashtraId, Code = "JN", Name = "Jalna", RegionType = "District", IsActive = true },
                new District { StateId = maharashtraId, Code = "HA", Name = "Hingoli", RegionType = "District", IsActive = true },
                new District { StateId = maharashtraId, Code = "HD", Name = "Hingoli", RegionType = "District", IsActive = true },
                new District { StateId = maharashtraId, Code = "GH", Name = "Gharipur", RegionType = "District", IsActive = true },
                new District { StateId = maharashtraId, Code = "GD", Name = "Gadchiroli", RegionType = "District", IsActive = true },
                new District { StateId = maharashtraId, Code = "GB", Name = "Gondia", RegionType = "District", IsActive = true },
                new District { StateId = maharashtraId, Code = "BR", Name = "Beed", RegionType = "District", IsActive = true },
                new District { StateId = maharashtraId, Code = "BL", Name = "Buldhana", RegionType = "District", IsActive = true },
                new District { StateId = maharashtraId, Code = "BN", Name = "Bhandup", RegionType = "District", IsActive = true },
                new District { StateId = maharashtraId, Code = "AH", Name = "Ahmednagar", RegionType = "District", IsActive = true }
            });

            // Delhi Districts (11)
            var delhiId = GetStateId("DL");
            districts.AddRange(new[]
            {
                new District { StateId = delhiId, Code = "NC", Name = "North Central", RegionType = "District", IsActive = true },
                new District { StateId = delhiId, Code = "NE", Name = "North East", RegionType = "District", IsActive = true },
                new District { StateId = delhiId, Code = "NT", Name = "North", RegionType = "District", IsActive = true },
                new District { StateId = delhiId, Code = "NW", Name = "North West", RegionType = "District", IsActive = true },
                new District { StateId = delhiId, Code = "CN", Name = "Central", RegionType = "District", IsActive = true },
                new District { StateId = delhiId, Code = "WS", Name = "West", RegionType = "District", IsActive = true },
                new District { StateId = delhiId, Code = "SW", Name = "South West", RegionType = "District", IsActive = true },
                new District { StateId = delhiId, Code = "SE", Name = "South East", RegionType = "District", IsActive = true },
                new District { StateId = delhiId, Code = "SN", Name = "South", RegionType = "District", IsActive = true },
                new District { StateId = delhiId, Code = "EA", Name = "East", RegionType = "District", IsActive = true },
                new District { StateId = delhiId, Code = "ES", Name = "South Delhi", RegionType = "District", IsActive = true }
            });

            // Karnataka Districts (31)
            var karnatakaId = GetStateId("KA");
            districts.AddRange(new[]
            {
                new District { StateId = karnatakaId, Code = "BG", Name = "Bengaluru Rural", RegionType = "District", IsActive = true },
                new District { StateId = karnatakaId, Code = "BU", Name = "Bengaluru Urban", RegionType = "District", IsActive = true },
                new District { StateId = karnatakaId, Code = "BB", Name = "Belagavi", RegionType = "District", IsActive = true },
                new District { StateId = karnatakaId, Code = "BH", Name = "Ballari", RegionType = "District", IsActive = true },
                new District { StateId = karnatakaId, Code = "BD", Name = "Bidar", RegionType = "District", IsActive = true },
                new District { StateId = karnatakaId, Code = "BP", Name = "Bijapur", RegionType = "District", IsActive = true },
                new District { StateId = karnatakaId, Code = "CH", Name = "Chikmagalur", RegionType = "District", IsActive = true },
                new District { StateId = karnatakaId, Code = "CK", Name = "Chikkaballapur", RegionType = "District", IsActive = true },
                new District { StateId = karnatakaId, Code = "CL", Name = "Chitradurga", RegionType = "District", IsActive = true },
                new District { StateId = karnatakaId, Code = "DB", Name = "Davangere", RegionType = "District", IsActive = true },
                new District { StateId = karnatakaId, Code = "DK", Name = "Dakshina Kannada", RegionType = "District", IsActive = true },
                new District { StateId = karnatakaId, Code = "GD", Name = "Gadag", RegionType = "District", IsActive = true },
                new District { StateId = karnatakaId, Code = "GU", Name = "Gulbarga", RegionType = "District", IsActive = true },
                new District { StateId = karnatakaId, Code = "HS", Name = "Hassan", RegionType = "District", IsActive = true },
                new District { StateId = karnatakaId, Code = "HD", Name = "Haveri", RegionType = "District", IsActive = true },
                new District { StateId = karnatakaId, Code = "KD", Name = "Kodagu", RegionType = "District", IsActive = true },
                new District { StateId = karnatakaId, Code = "KG", Name = "Kolar", RegionType = "District", IsActive = true },
                new District { StateId = karnatakaId, Code = "KP", Name = "Koppal", RegionType = "District", IsActive = true },
                new District { StateId = karnatakaId, Code = "KU", Name = "Kurnool", RegionType = "District", IsActive = true },
                new District { StateId = karnatakaId, Code = "MG", Name = "Mandya", RegionType = "District", IsActive = true },
                new District { StateId = karnatakaId, Code = "MY", Name = "Mysuru", RegionType = "District", IsActive = true },
                new District { StateId = karnatakaId, Code = "RB", Name = "Ramanagara", RegionType = "District", IsActive = true },
                new District { StateId = karnatakaId, Code = "RT", Name = "Raichur", RegionType = "District", IsActive = true },
                new District { StateId = karnatakaId, Code = "SH", Name = "Shimoga", RegionType = "District", IsActive = true },
                new District { StateId = karnatakaId, Code = "TM", Name = "Tumkur", RegionType = "District", IsActive = true },
                new District { StateId = karnatakaId, Code = "UC", Name = "Udupi", RegionType = "District", IsActive = true },
                new District { StateId = karnatakaId, Code = "UT", Name = "Uttara Kannada", RegionType = "District", IsActive = true },
                new District { StateId = karnatakaId, Code = "VJ", Name = "Vijayapura", RegionType = "District", IsActive = true },
                new District { StateId = karnatakaId, Code = "YD", Name = "Yadgir", RegionType = "District", IsActive = true },
                new District { StateId = karnatakaId, Code = "WN", Name = "Chamarajanagar", RegionType = "District", IsActive = true }
            });

            // Add Tamil Nadu districts (38)
            var tamilNaduId = GetStateId("TN");
            districts.AddRange(new[]
            {
                new District { StateId = tamilNaduId, Code = "CH", Name = "Chennai", RegionType = "District", IsActive = true },
                new District { StateId = tamilNaduId, Code = "CN", Name = "Chengalpattu", RegionType = "District", IsActive = true },
                new District { StateId = tamilNaduId, Code = "CR", Name = "Cuddalore", RegionType = "District", IsActive = true },
                new District { StateId = tamilNaduId, Code = "CY", Name = "Coimb atore", RegionType = "District", IsActive = true },
                new District { StateId = tamilNaduId, Code = "DH", Name = "Dharmapuri", RegionType = "District", IsActive = true },
                new District { StateId = tamilNaduId, Code = "DR", Name = "Dindigul", RegionType = "District", IsActive = true },
                new District { StateId = tamilNaduId, Code = "ER", Name = "Erode", RegionType = "District", IsActive = true },
                new District { StateId = tamilNaduId, Code = "KN", Name = "Kanchipuram", RegionType = "District", IsActive = true },
                new District { StateId = tamilNaduId, Code = "KY", Name = "Kanyakumari", RegionType = "District", IsActive = true },
                new District { StateId = tamilNaduId, Code = "KA", Name = "Karur", RegionType = "District", IsActive = true },
                new District { StateId = tamilNaduId, Code = "KD", Name = "Krishnagiri", RegionType = "District", IsActive = true },
                new District { StateId = tamilNaduId, Code = "MD", Name = "Madurai", RegionType = "District", IsActive = true },
                new District { StateId = tamilNaduId, Code = "MG", Name = "Mayiladuthurai", RegionType = "District", IsActive = true },
                new District { StateId = tamilNaduId, Code = "NK", Name = "Nagapattinam", RegionType = "District", IsActive = true },
                new District { StateId = tamilNaduId, Code = "NL", Name = "Nellore", RegionType = "District", IsActive = true },
                new District { StateId = tamilNaduId, Code = "NM", Name = "Namakkal", RegionType = "District", IsActive = true },
                new District { StateId = tamilNaduId, Code = "PN", Name = "Perambalur", RegionType = "District", IsActive = true },
                new District { StateId = tamilNaduId, Code = "PD", Name = "Puducherry", RegionType = "District", IsActive = true },
                new District { StateId = tamilNaduId, Code = "RK", Name = "Ramanathapuram", RegionType = "District", IsActive = true },
                new District { StateId = tamilNaduId, Code = "RG", Name = "Ranipet", RegionType = "District", IsActive = true },
                new District { StateId = tamilNaduId, Code = "SF", Name = "Salem", RegionType = "District", IsActive = true },
                new District { StateId = tamilNaduId, Code = "SR", Name = "Sivaganga", RegionType = "District", IsActive = true },
                new District { StateId = tamilNaduId, Code = "TK", Name = "Tenkasi", RegionType = "District", IsActive = true },
                new District { StateId = tamilNaduId, Code = "TN", Name = "Thanjavur", RegionType = "District", IsActive = true },
                new District { StateId = tamilNaduId, Code = "TI", Name = "Theni", RegionType = "District", IsActive = true },
                new District { StateId = tamilNaduId, Code = "TR", Name = "Thiruvannamalai", RegionType = "District", IsActive = true },
                new District { StateId = tamilNaduId, Code = "TV", Name = "Thiruvallur", RegionType = "District", IsActive = true },
                new District { StateId = tamilNaduId, Code = "TU", Name = "Tirupathur", RegionType = "District", IsActive = true },
                new District { StateId = tamilNaduId, Code = "TP", Name = "Tiruppur", RegionType = "District", IsActive = true },
                new District { StateId = tamilNaduId, Code = "TC", Name = "Tiruvannamalai", RegionType = "District", IsActive = true },
                new District { StateId = tamilNaduId, Code = "TY", Name = "Tuticorin", RegionType = "District", IsActive = true },
                new District { StateId = tamilNaduId, Code = "VL", Name = "Vellore", RegionType = "District", IsActive = true },
                new District { StateId = tamilNaduId, Code = "VI", Name = "Villupuram", RegionType = "District", IsActive = true },
                new District { StateId = tamilNaduId, Code = "VR", Name = "Virudunagar", RegionType = "District", IsActive = true },
                new District { StateId = tamilNaduId, Code = "AK", Name = "Ariyalur", RegionType = "District", IsActive = true },
                new District { StateId = tamilNaduId, Code = "KL", Name = "Kallakurichi", RegionType = "District", IsActive = true },
                new District { StateId = tamilNaduId, Code = "CP", Name = "Chengalpattu", RegionType = "District", IsActive = true }
            });

            return districts;
        }
    }
}
