﻿using System;
using System.Linq;
using TaskCat.Common.Settings;
using TaskCat.Data.Model.Geocoding;
using TaskCat.Data.Model.Order.Delivery;
using TaskCat.Data.Model.Person;
using TaskCat.PartnerModels.Infini;
using TaskCat.Payment.Core.Manual;

namespace TaskCat.BackgroundJobService
{
    public static class OrderExtensions
    {
        public static ClassifiedDeliveryOrder ToClassifiedDeliveryOrder(
            this Order infiniOrder,
            ProprietorSettings defaultAddressSettings)
        {
            if (infiniOrder == null)
            {
                throw new ArgumentNullException(nameof(infiniOrder));
            }

            const string orderPrefix = "IFA";
            ClassifiedDeliveryOrder taskcatOrder = new ClassifiedDeliveryOrder(
                orderPrefix + "-" + infiniOrder.id)
            {
                IsAutogenerated = true,
                ReferenceOrderId = infiniOrder.id.ToString(),
                Description = infiniOrder.description,

                PaymentMethod = CashOnDeliveryPaymentMethod.PaymentMethodKey,

                OrderCart = new OrderDetails
                {
                    ServiceCharge = 150, // TODO: This needs some lookup here.
                    SubTotal = infiniOrder.order_amt
                }
            };

            // Infini doesn't provide a from address for now. We are setting it to a configured default
            taskcatOrder.From = defaultAddressSettings.Address;
            taskcatOrder.SellerInfo = new PersonInfo()
            {
                Name = defaultAddressSettings.Name,
                PhoneNumber = defaultAddressSettings.PhoneNumber
            };

            taskcatOrder.To = new DefaultAddress(
                addressLine1: infiniOrder.address1,
                addressLine2: infiniOrder.address2,
                locality: infiniOrder.zone_id,
                postcode: infiniOrder.postal_code,
                city: infiniOrder.city,
                country: "Bangladesh", // TODO: Infini sends back a country code, either we need to parse it or find a way to make this nice
                point: null);

            taskcatOrder.BuyerInfo = new PersonInfo()
            {
                Name = infiniOrder.first_name + " " + infiniOrder.last_name,
                PhoneNumber = infiniOrder.phone_no
            };

            taskcatOrder.OrderCart.TotalToPay = infiniOrder.pay_amt + taskcatOrder.OrderCart.ServiceCharge;
            taskcatOrder.OrderCart.PackageList = infiniOrder.cart.Select(x =>
                new ItemDetails()
                {
                    Item = x.Value.name,
                    Price = Decimal.Parse(x.Value.price),
                    Quantity = Int32.Parse(x.Value.qty)
                }).ToList();

            return taskcatOrder;
        }
    }
}
