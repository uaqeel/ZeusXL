/* Copyright (C) 2019 Interactive Brokers LLC. All rights reserved. This code is subject to the terms
 * and conditions of the IB API Non-Commercial License or the IB API Commercial License, as applicable. */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IBApi;

namespace CommonTypes.Messages
{
    public class OpenOrderMessage : OrderMessage
    {
        private IBApi.Contract contract;
        private IBApi.Order order;
        private OrderState orderState;

        public OpenOrderMessage(int orderId, IBApi.Contract contract, IBApi.Order order, OrderState orderState)
        {
            OrderId = orderId;
            Contract = contract;
            Order = order;
            OrderState = orderState;
        }
        
        public IBApi.Contract Contract
        {
            get { return contract; }
            set { contract = value; }
        }
        
        public IBApi.Order Order
        {
            get { return order; }
            set { order = value; }
        }
        
        public OrderState OrderState
        {
            get { return orderState; }
            set { orderState = value; }
        }
        
    }
}
