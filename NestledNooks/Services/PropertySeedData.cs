using NestledNooks.Data;

namespace NestledNooks.Services;

public static class PropertySeedData
{
    public const string DeerfieldSlug = "deerfield-retreat";

    public static RentalProperty CreateDeerfieldRetreat()
    {
        var photos = new[]
        {
            ("/images/322EliFrontArial01.webp", "Front aerial view"),
            ("/images/322EliBalcony_1.webp", "Back Balcony"),
            ("/images/322EliLiving_Dining_1.webp", "Living Room view"),
            ("/images/322EliKitchen_1.webp", "Kitchen View"),
            ("/images/322DiningRoomView.webp", "Dining Room View"),
            ("/images/322SwingView_1.webp", "Swing/Ground View"),
            ("/images/322MasterBed_1.webp", "Master Bedroom"),
            ("/images/322EliArialBack04.webp", "Exterior aerial view"),
            ("/images/322EliFrontArial02.webp", "Front aerial view"),
            ("/images/322EliBackArial01.webp", "Back aerial view"),
            ("/images/322EliBackArial02.webp", "Back aerial view"),
            ("/images/322EliBackArial03.webp", "Back aerial view"),
            ("/images/322EliArial05.webp", "Aerial view"),
            ("/images/322EliFront01.webp", "Front exterior"),
            ("/images/322EliFront02.webp", "Front exterior"),
            ("/images/322EliFront03.webp", "Front exterior"),
            ("/images/322EliEntryway.webp", "Entryway"),
            ("/images/322EliDrLiving1CouchBed_01.webp", "Living room with sofa bed"),
            ("/images/322EliDrLiving1_01.webp", "Living room - Upstairs"),
            ("/images/322EliDrLiving1_02.webp", "Living room - Upstairs"),
            ("/images/322EliDrLiving1_03.webp", "Living room - Upstairs"),
            ("/images/322EliDrLiving1_04.webp", "Living room - Upstairs"),
            ("/images/322EliDrLiving1_05.webp", "Living room - Upstairs"),
            ("/images/322EliLiving2_1.webp", "Living room - Downstairs"),
            ("/images/322EliLiving2_2.webp", "Living room - Downstairs"),
            ("/images/322EliLiving2_3.webp", "Living room - Downstairs"),
            ("/images/322EliLiving2_4.webp", "Living room - Downstairs"),
            ("/images/322EliLiving2_5.webp", "Living room - Downstairs"),
            ("/images/322EliDining1_01.webp", "Dining area - Upstairs"),
            ("/images/322EliDining2_1.webp", "Dining area - Downstairs"),
            ("/images/322EliDining2_2.webp", "Dining area - Downstairs"),
            ("/images/322EliDining2_3.webp", "Dining area - Downstairs"),
            ("/images/322EliDining2_4.webp", "Dining area - Downstairs"),
            ("/images/322EliKitchen_01.webp", "Kitchen"),
            ("/images/322EliKitchen_02.webp", "Kitchen"),
            ("/images/322EliKitchen_03.webp", "Kitchen"),
            ("/images/322EliKitchen_04.webp", "Kitchen"),
            ("/images/322EliKitchen_1.webp", "Kitchen"),
            ("/images/322EliMasterBath_1.webp", "Master bathroom"),
            ("/images/322EliMasterBath_1_alt.webp", "Master bathroom"),
            ("/images/322EliMasterBedKing_1.webp", "Master bedroom"),
            ("/images/322EliMasterBedKing_2.webp", "Master bedroom"),
            ("/images/322EliMasterBedKing_3.webp", "Master bedroom"),
            ("/images/322EliMasterBedKing_4.webp", "Master bedroom"),
            ("/images/322EliMasterBedKing_5.webp", "Master bedroom"),
            ("/images/322EliMasterCloset.webp", "Master closet"),
            ("/images/322EliBed3wKing_1.webp", "Bedroom 3 with king bed"),
            ("/images/322EliBed3wKing_2.webp", "Bedroom 3 with king bed"),
            ("/images/322EliBed3wKing_3.webp", "Bedroom 3 with king bed"),
            ("/images/322EliBed3wKing_4.webp", "Bedroom 3 with king bed"),
            ("/images/322EliBed3wKing_5.webp", "Bedroom 3 with king bed"),
            ("/images/322EliBed3wKing_6.webp", "Bedroom 3 with king bed"),
            ("/images/322EliBed3wKing_7.webp", "Bedroom 3 with king bed"),
            ("/images/322EliBed4_1.webp", "Bedroom 4"),
            ("/images/322EliBed4_2.webp", "Bedroom 4"),
            ("/images/322EliBed4_3.webp", "Bedroom 4"),
            ("/images/322EliBed4_4.webp", "Bedroom 4"),
            ("/images/322EliBedroom2_1.webp", "Bedroom 2"),
            ("/images/322EliBedroom2_2.webp", "Bedroom 2"),
            ("/images/322EliBedroom2_3.webp", "Bedroom 2"),
            ("/images/322EliBedroom2_4.webp", "Bedroom 2"),
            ("/images/322EliBedroom2_5.webp", "Bedroom 2"),
            ("/images/322EliBath2_1.webp", "Bathroom 2"),
            ("/images/322EliBathroom3.webp", "Bathroom 3"),
            ("/images/322EliBalcony_2.webp", "Back balcony"),
            ("/images/322EliBalcony_3.webp", "Back balcony"),
            ("/images/322EliBalcony_4.webp", "Back balcony"),
            ("/images/322EliBack01.webp", "Back yard"),
            ("/images/322EliBackPorch_01.webp", "Back porch"),
            ("/images/322EliGarage.webp", "Garage"),
            ("/images/322EliLaundryRoom.webp", "Laundry room"),
            ("/images/322EliRecordPlayer.webp", "Record player"),
        };

        return new RentalProperty
        {
            Slug = DeerfieldSlug,
            DisplayName = "Deerfield Retreat",
            IsPublished = true,
            IsHomepage = true,
            SortOrder = 0,
            MetaDescription =
                "Peaceful vacation home near Rapid City, South Dakota — open fields, visiting deer, sleeps up to 12, 4 bedrooms, 3 baths. Relax and recharge at Deerfield Retreat.",
            Subtitle =
                "A peaceful getaway surrounded by open fields and visiting deer — the perfect spot to slow down, recharge, and enjoy the quiet.",
            StatsJson = PropertyContentJson.SerializeStringList(
            [
                "Sleeps 12",
                "4 bedrooms",
                "6 beds",
                "3 baths",
                "Rapid City",
                "30m from Sturgis",
            ]),
            TagsLine1 = "Entire home · Black Hills, South Dakota",
            TagsLine2 = "Family-friendly · Prairie views · Quiet nights",
            BadgesJson = PropertyContentJson.SerializeBadges(
            [
                new PropertyBadge { Title = "Guest favorite", Subtitle = "A relaxing, family-friendly stay" },
                new PropertyBadge { Title = "Prairie view", Subtitle = "Open fields + big skies" },
            ]),
            AboutText =
                "Deerfield Retreat is a cozy escape tucked away from the noise, with wide-open views and frequent deer sightings right from the yard. Whether you're here with family, friends, or on a solo reset, it's designed to feel calm, comfortable, and easy.",
            AmenitiesJson = PropertyContentJson.SerializeStringList(
            [
                "4 comfortable bedrooms with scenic views",
                "Fully equipped kitchen with modern appliances",
                "Outdoor seating area with BBQ grill",
                "High-speed Wi‑Fi for work or streaming",
                "Washer & dryer for longer stays",
                "Pet-friendly (with prior approval)",
            ]),
            LocationText =
                "Deerfield Retreat is in north Rapid City, so you're close to groceries, restaurants, and attractions — but far enough out that nights stay quiet and the stars stay bright.",
            GuideTeaserText =
                "House details, local recommendations, and stay resources. Wi‑Fi and access info are in your confirmation email.",
            BookingSubtext = "Direct request, Airbnb, or Vrbo — calendars stay in sync.",
            BookingFinePrint = "Most accurate pricing and availability appear on each platform.",
            AirbnbUrl = "https://airbnb.com/h/mydeerfieldretreat",
            VrboUrl = "https://www.vrbo.com/4507873?dateless=true",
            PhotosJson = PropertyContentJson.SerializePhotos(
                photos.Select(p => new PropertyPhoto { Url = p.Item1, Alt = p.Item2 })),
            UpdatedAtUtc = DateTime.UtcNow,
        };
    }
}
