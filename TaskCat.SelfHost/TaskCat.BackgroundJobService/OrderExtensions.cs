﻿using System;
using System.Collections.Generic;
using System.Linq;
using TaskCat.Common.Settings;
using TaskCat.Data.Model.Geocoding;
using TaskCat.Data.Model.Inventory;
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
            ProprietorSettings defaultAddressSettings,
            string userId)
        {
            if (infiniOrder == null)
            {
                throw new ArgumentNullException(nameof(infiniOrder));
            }

            if (defaultAddressSettings == null)
            {
                throw new ArgumentNullException(nameof(defaultAddressSettings));
            }

            if (userId == null)
            {
                throw new ArgumentNullException(nameof(userId));
            }

            const string orderPrefix = "IFA";
            ClassifiedDeliveryOrder taskcatOrder = new ClassifiedDeliveryOrder(
                orderPrefix + "-" + infiniOrder.id)
            {
                IsAutogenerated = true,
                ReferenceOrderId = infiniOrder.id.ToString(),
                Description = GenerateTaskCatJobDescription(infiniOrder),

                PaymentMethod = CashOnDeliveryPaymentMethod.PaymentMethodKey,

                OrderCart = new OrderDetails
                {
                    ServiceCharge = 150, // TODO: This needs some lookup here.
                    SubTotal = infiniOrder.total
                }
            };

            taskcatOrder.OrderCart.TotalToPay = infiniOrder.total + taskcatOrder.OrderCart.ServiceCharge;
            taskcatOrder.OrderCart.PackageList = new List<ItemDetails>();

            for (int i = 0; i < infiniOrder.qty; i++)
            {
                taskcatOrder.OrderCart.PackageList.Add(new ItemDetails()
                {
                    Item = infiniOrder.product,
                    Price = infiniOrder.unit_price,
                    Quantity = infiniOrder.qty
                });
            }

            // Infini doesn't provide a from address for now. We are setting it to a configured default
            if (infiniOrder.vendor_address == null || !infiniOrder.vendor_address.Any())
            {
                taskcatOrder.From = defaultAddressSettings.Address;
            }
            else
            {
                var vendorAddress = infiniOrder.vendor_address.First();
                taskcatOrder.From = new DefaultAddress()
                {
                    AddressLine1 = vendorAddress.address1,
                    AddressLine2 = String.IsNullOrWhiteSpace(vendorAddress.address3) ? vendorAddress.address2 : string.Join(" ", vendorAddress.address2, vendorAddress.address3),
                };
            }

            taskcatOrder.SellerInfo = new PersonInfo()
            {
                Name = defaultAddressSettings.Name,
                PhoneNumber = defaultAddressSettings.PhoneNumber
            };

            taskcatOrder.UserId = userId;

            taskcatOrder.To = new DefaultAddress(
                addressLine1: infiniOrder.user_address.address1,
                addressLine2: string.IsNullOrWhiteSpace(infiniOrder.user_address.address3) ? infiniOrder.user_address.address2 : string.Join(" ", infiniOrder.user_address.address2, infiniOrder.user_address.address3),
                locality: infiniOrder.user_address.zone,
                postcode: infiniOrder.user_address.postcode,
                city: infiniOrder.user_address.city,
                country: "Bangladesh", // TODO: Infini sends back a country code, either we need to parse it or find a way to make this nice
                point: null);

            taskcatOrder.BuyerInfo = new PersonInfo()
            {
                Name = string.Join(" ", infiniOrder.user_address.firstname, infiniOrder.user_address.lastname),
                PhoneNumber = infiniOrder.user_address.phone_no
            };

            taskcatOrder.Variant = DeliveryOrderVariants.EnterpriseDelivery;

            return taskcatOrder;
        }

        private static string GenerateTaskCatJobDescription(Order infiniOrder)
        {
            if (infiniOrder == null)
            {
                throw new ArgumentNullException(nameof(infiniOrder));
            }

            return $"IFA order {infiniOrder.id} for {infiniOrder.product}";
        }
    }
}
