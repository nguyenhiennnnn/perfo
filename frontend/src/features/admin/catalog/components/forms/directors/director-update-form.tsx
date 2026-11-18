import { VALIDATION_MESSAGES } from "@/config/error-messages";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";

import { FieldGroup } from "@/components/ui/field";
import { FormInputField } from "@/components/common/form/form-input-field";

import { useAdminUpdateDirector } from "@/features/admin/catalog/hooks/use-admin-director";
import type { DirectorDto } from "@/features/admin/catalog/types";
import { URL_PLACEHOLDER, PERSON_NAME_MAX_LENGTH, IMAGE_URL_MAX_LENGTH, FORM_SAVE_CHANGES_LABEL } from "@/config/default-constants";
import { FormActions } from "@/components/common/form/form-actions";
import { useServerFieldErrors } from "@/hooks/use-server-field-errors";

const directorFormSchema = z.object({
  fullName: z
    .string()
    .min(1, VALIDATION_MESSAGES.fullNameRequired)
    .max(PERSON_NAME_MAX_LENGTH, VALIDATION_MESSAGES.fullNameTooLong(PERSON_NAME_MAX_LENGTH)),
  avatarUrl: z
    .string()
    .url(VALIDATION_MESSAGES.imageUrlInvalid)
    .max(IMAGE_URL_MAX_LENGTH, VALIDATION_MESSAGES.imageUrlTooLong(IMAGE_URL_MAX_LENGTH))
    .optional()
    .or(z.literal("")),
});

type DirectorFormValues = z.infer<typeof directorFormSchema>;

interface DirectorUpdateFormProps {
  director: DirectorDto;
  onSuccess: () => void;
}

export const DirectorUpdateForm = ({
  director,
  onSuccess,
}: DirectorUpdateFormProps) => {
  const updateDirector = useAdminUpdateDirector();

  const form = useForm<DirectorFormValues>({
    resolver: zodResolver(directorFormSchema),
    defaultValues: {
      fullName: director.fullName,
      avatarUrl: director.avatarUrl ?? "",
    },
  });

  const applyServerErrors = useServerFieldErrors(form);

  const onSubmit = (values: DirectorFormValues) => {
    updateDirector.mutate({
      id: director.directorId,
      body: {
        fullName: values.fullName,
        avatarUrl: values.avatarUrl || null,
      },
    }, {
      onSuccess: () => onSuccess(),
      onError: applyServerErrors,
    });
  };

  return (
    <form
      id="director-update-form"
      onSubmit={form.handleSubmit(onSubmit)}
      className="space-y-4"
    >
      <FieldGroup disabled={updateDirector.isPending}>
        <FormInputField
          name="fullName"
          control={form.control}
          label="Họ và tên"
          placeholder="Nhập họ và tên đạo diễn"
          autoFocus
          autoComplete="off"
        />

        <FormInputField
          name="avatarUrl"
          control={form.control}
          label="Ảnh đại diện (không bắt buộc)"
          placeholder={URL_PLACEHOLDER}
          autoComplete="off"
        />
      </FieldGroup>

      <FormActions
        isPending={updateDirector.isPending}
        isDirty={form.formState.isDirty}
        submitLabel={FORM_SAVE_CHANGES_LABEL}
        onCancel={onSuccess}
      />
    </form>
  );
};