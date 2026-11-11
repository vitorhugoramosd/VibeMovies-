import React from 'react';
import { Modal } from './Modal';
import { AuthForm } from './AuthForm';

interface AuthModalProps {
  open: boolean;
  onClose: () => void;
}

export const AuthModal: React.FC<AuthModalProps> = ({ open, onClose }) => (
  <Modal open={open} onClose={onClose} labelledBy="auth-modal-title" panelClassName="w-full max-w-md p-6 sm:p-8">
    <AuthForm titleId="auth-modal-title" onSuccess={onClose} />
  </Modal>
);
