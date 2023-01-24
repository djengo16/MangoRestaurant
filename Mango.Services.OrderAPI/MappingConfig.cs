using AutoMapper;
using Mango.Services.OrderAPI.Messages;
using Mango.Services.OrderAPI.Models;

namespace Mango.Services.OrderAPI
{
    public class MappingConfig
    {
        public static MapperConfiguration RegisterMaps()
        {
            var mappingConfig = new MapperConfiguration(config =>
            {
                config.CreateMap<CartDetailsDto, OrderDetails>()
                .ForMember(
                    member => member.OrderDetailsId,
                    opt => opt.Ignore())
                .ForMember(
                    member => member.OrderHeaderId,
                    opt => opt.Ignore()); ;

                config.CreateMap<CheckoutHeaderDto, OrderHeader>()
                .ForMember(
                    member => member.OrderTime,
                    opt => opt.MapFrom(x => DateTime.Now))
                .ForMember(
                    member => member.PaymentStatus,
                    opt => opt.MapFrom(x => false))
                .ForMember(
                    member => member.OrderDetails,
                    opt => opt.MapFrom(x => x.CartDetails))
                .ForMember(
                    member => member.CartTotalItems,
                    opt => opt.MapFrom(x => x.CartDetails.Count()));
            });

            return mappingConfig;
        }
    }
}
