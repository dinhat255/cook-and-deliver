namespace CookAndDeliver.Cooking.Data
{
    // Tuyen duong xu ly nhiet cua nguyen lieu
    public enum CookingRoute
    {
        Direct, // Khong can nau, nem truc tiep vao thot (Banh pho, Hanh, Kim chi...)
        Grill,  // Can nuong tren vi than (Thit bo, Thit heo)
        Oven,   // Can ep nong trong lo (Banh mi)
        Pan     // Can xao sot trong chao (Banh gao Tteok)
    }
}
