import { z } from "zod";
import { VALIDATION_MESSAGES } from "@/config/error-messages";
import { EMAIL_MAX_LENGTH } from "@/config/default-constants";
import { INVOICE_ADDRESS_MAX_LENGTH, INVOICE_COMPANY_NAME_MAX_LENGTH } from "../constants";
import type { UpsertInvoiceProfileRequest } from "../types";

const TAX_CODE_PATTERN = /^\d{10}(-\d{3})?$/;

export const invoiceInfoSchema = z.object({
  taxCode: z
    .string()
    .trim()
    .min(1, "Vui lòng nhập mã số thuế.")
    .regex(TAX_CODE_PATTERN, "Mã số thuế gồm 10 chữ số, hoặc 10 chữ số kèm hậu tố -XXX."),
  companyName: z
    .string()
    .trim()
    .min(1, "Vui lòng nhập tên đơn vị.")
    .max(INVOICE_COMPANY_NAME_MAX_LENGTH, `Tên đơn vị tối đa ${INVOICE_COMPANY_NAME_MAX_LENGTH} ký tự.`),
  address: z
    .string()
    .trim()
    .min(1, "Vui lòng nhập địa chỉ.")
    .max(INVOICE_ADDRESS_MAX_LENGTH, `Địa chỉ tối đa ${INVOICE_ADDRESS_MAX_LENGTH} ký tự.`),
  email: z
    .string()
    .trim()
    .min(1, VALIDATION_MESSAGES.emailRequired)
    .email(VALIDATION_MESSAGES.emailInvalid)
    .max(EMAIL_MAX_LENGTH, `Email tối đa ${EMAIL_MAX_LENGTH} ký tự.`),
});

export type InvoiceInfoFormValues = z.infer<typeof invoiceInfoSchema>;

export const EMPTY_INVOICE_INFO: InvoiceInfoFormValues = {
  taxCode: "",
  companyName: "",
  address: "",
  email: "",
};

export const toInvoiceInfoRequest = (values: InvoiceInfoFormValues): UpsertInvoiceProfileRequest => ({
  taxCode: values.taxCode.trim(),
  companyName: values.companyName.trim(),
  address: values.address.trim(),
  email: values.email.trim(),
});
