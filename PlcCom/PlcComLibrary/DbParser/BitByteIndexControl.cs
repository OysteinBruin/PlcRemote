﻿using PlcComLibrary.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlcComLibrary.DbParser
{
    public class BitByteIndexControl
    {
        private DbFileLineItem _previousLineItem;
        private DbFileLineItem _previousDataTypeLineItem;

        public BitByteIndexControl()
        {
            Reset();
        }
        
        public int ByteCounter { get; private set; }
        public int BitCounter { get; private set; }

        public void Update(DbFileLineItem lineItem)
        {
            if (_previousLineItem == null)
            {
                BitCounter = 0;
                ByteCounter = 0;
            }
            else
            {
                if (lineItem.IsDataType)
                {
                    if (lineItem.IsBoolType)
                    {
                        HandleBooleanType();
                    }
                    else
                    {
                        HandleNumericType();
                    }
                }
            }

             _previousLineItem = lineItem;

            if (lineItem.IsDataType)
            {
                _previousDataTypeLineItem = lineItem;
            }
        }

        public void Reset()
        {
            BitCounter = 0;
            ByteCounter = 0;
            _previousDataTypeLineItem = new DbFileLineItem();
            _previousLineItem = null;
        }

        private void HandleBooleanType()
        {
            if (_previousLineItem.IsDataType)
            {
                if (_previousLineItem.IsBoolType)
                {
                    BitCounter++;
                    if (BitCounter > Constants.LastBitInByte)
                    {
                        BitCounter = 0;
                        ByteCounter++;
                    }
                }
                else
                {
                    BitCounter = 0;
                    ByteCounter += Constants.S7DataTypesByteSize[_previousLineItem.DataTypeStr];
                }
            }
            else
            {
                BitCounter = 0;
                HandlePreviousIterationWasNonDataType();
            }
        }

        private void HandleNumericType()
        {
            BitCounter = 0;

            if (_previousLineItem.IsDataType)
            {
                if (_previousLineItem.IsBoolType)
                {
                    ByteCounter += (ByteCounter % 2 == 0 ? 2 : 1);
                }
                else
                {
                    ByteCounter += Constants.S7DataTypesByteSize[_previousLineItem.DataTypeStr];
                }
            }
            else
            {
                HandlePreviousIterationWasNonDataType();
            }
        }

        private void HandlePreviousIterationWasNonDataType()
        {
            if (_previousDataTypeLineItem.IsDataType)
            {
                int previousDataTypeLineItemBytesize = Constants.S7DataTypesByteSize[_previousDataTypeLineItem.DataTypeStr];

                ByteCounter += previousDataTypeLineItemBytesize;

                if (ByteCounter % 2 != 0)
                {
                    ByteCounter += 1;
                }
            }
            else
            {
                ByteCounter += (ByteCounter % 2 == 0 ? 2 : 1);
            }
        }
    }
}
