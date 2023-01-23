namespace Mango.Web.Services.IServices
{
    public interface ICouponService
    {
        Task<T> GetCouponAsync<T>(string couponCode, string token = null);
    }
}
