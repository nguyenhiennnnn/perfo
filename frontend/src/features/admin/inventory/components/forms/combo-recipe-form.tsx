import { useForm, FormProvider } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";

import { FieldGroup, Field, FieldLabel } from "@/components/ui/field";
import { FormActions } from "@/components/common/form/form-actions";
import { QueryState } from "@/components/common/query-state";
import { FormSkeleton } from "@/components/common/skeleton-shapes";
import { useServerFieldErrors } from "@/hooks/use-server-field-errors";
import { FORM_SAVE_CHANGES_LABEL, LOOKUP_PAGE_SIZE } from "@/config/default-constants";
import { useAdminCombos } from "@/features/admin/combos/hooks/use-admin-combo";

import {
  useAdminComboRecipe,
  useAdminComboRecipes,
  useAdminUpsertComboRecipe,
} from "../../hooks/use-admin-inventory";
import type { ComboRecipeDto } from "../../types";
import {
  EMPTY_INVENTORY_LINE,
  comboRecipeFormSchema,
  type ComboRecipeFormValues,
} from "../../utils/inventory-schema";
import { InventoryLinesField } from "./inventory-lines-field";
import { OptionSelectField } from "./option-select-field";

interface ComboRecipeFormProps {
  recipe?: ComboRecipeDto;
  onSuccess: () => void;
}

const COMBO_LOOKUP_PARAMS = { page: 1, pageSize: LOOKUP_PAGE_SIZE };

export const ComboRecipeForm = ({ recipe, onSuccess }: ComboRecipeFormProps) => {
  const isEdit = Boolean(recipe);
  const upsertRecipe = useAdminUpsertComboRecipe();
  const combos = useAdminCombos(COMBO_LOOKUP_PARAMS);
  const recipes = useAdminComboRecipes(COMBO_LOOKUP_PARAMS);

  const form = useForm<ComboRecipeFormValues>({
    resolver: zodResolver(comboRecipeFormSchema),
    defaultValues: {
      comboId: recipe ? String(recipe.comboId) : "",
      lines: recipe
        ? recipe.lines.map((line) => ({
            inventoryItemId: String(line.inventoryItemId),
            quantity: String(line.quantity),
            unitCost: "",
          }))
        : [EMPTY_INVENTORY_LINE],
    },
  });

  const applyServerErrors = useServerFieldErrors(form);

  const takenComboIds = new Set((recipes.data?.items ?? []).map((item) => item.comboId));
  const comboOptions = (combos.data?.items ?? [])
    .filter((combo) => !takenComboIds.has(combo.comboId))
    .map((combo) => ({ value: String(combo.comboId), label: combo.name }));

  const onSubmit = (values: ComboRecipeFormValues) => {
    upsertRecipe.mutate(
      {
        comboId: Number(values.comboId),
        body: {
          lines: values.lines.map((line) => ({
            inventoryItemId: Number(line.inventoryItemId),
            quantity: Number(line.quantity),
          })),
        },
      },
      { onSuccess: () => onSuccess(), onError: applyServerErrors },
    );
  };

  return (
    <FormProvider {...form}>
      <form id="combo-recipe-form" onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
        <FieldGroup disabled={upsertRecipe.isPending}>
          {recipe ? (
            <Field>
              <FieldLabel>Combo</FieldLabel>
              <p className="text-sm font-medium text-foreground">{recipe.comboName}</p>
            </Field>
          ) : (
            <OptionSelectField
              name="comboId"
              control={form.control}
              label="Combo"
              placeholder="Chọn combo chưa có công thức"
              options={comboOptions}
            />
          )}
          <InventoryLinesField mode="recipe" />
        </FieldGroup>

        <FormActions
          isPending={upsertRecipe.isPending}
          isDirty={form.formState.isDirty}
          submitLabel={isEdit ? FORM_SAVE_CHANGES_LABEL : "Lưu công thức"}
          onCancel={onSuccess}
        />
      </form>
    </FormProvider>
  );
};

interface ComboRecipeEditContentProps {
  comboId: number;
  onSuccess: () => void;
}

export const ComboRecipeEditContent = ({ comboId, onSuccess }: ComboRecipeEditContentProps) => {
  const recipe = useAdminComboRecipe(comboId);

  return (
    <QueryState
      query={recipe}
      skeleton={<FormSkeleton fields={4} />}
      errorTitle="Không tải được công thức combo"
    >
      {(data) => <ComboRecipeForm recipe={data} onSuccess={onSuccess} />}
    </QueryState>
  );
};
