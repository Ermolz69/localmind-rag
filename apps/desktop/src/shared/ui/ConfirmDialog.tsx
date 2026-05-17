import { Button } from "./Button";
import { Modal } from "./Modal";

type ConfirmDialogProps = {
  open: boolean;
  title: string;
  description: string;
  confirmLabel?: string;
  cancelLabel?: string;
  isPending?: boolean;
  onConfirm: () => void;
  onClose: () => void;
};

export function ConfirmDialog({
  open,
  title,
  description,
  confirmLabel = "Confirm",
  cancelLabel = "Cancel",
  isPending = false,
  onConfirm,
  onClose,
}: ConfirmDialogProps) {
  return (
    <Modal
      open={open}
      title={title}
      description={description}
      onClose={onClose}
    >
      <div className="flex justify-end gap-2">
        <Button variant="secondary" onClick={onClose} disabled={isPending}>
          {cancelLabel}
        </Button>
        <Button onClick={onConfirm} disabled={isPending}>
          {isPending ? "Working..." : confirmLabel}
        </Button>
      </div>
    </Modal>
  );
}
