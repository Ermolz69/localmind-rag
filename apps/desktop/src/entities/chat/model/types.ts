import type { RagSource } from "@entities/source";

export type ChatConversation = {
  id: string;
  title: string;
  createdAt?: string;
  updatedAt?: string | null;
};

export type ChatMessageDto = {
  id: string;
  conversationId: string;
  role: "User" | "Assistant" | "user" | "assistant" | string;
  content: string;
  createdAt: string;
  updatedAt: string | null;
};

export type CreateConversationRequest = {
  title: string;
};

export type UpdateConversationRequest = {
  title: string;
};

export type RagAnswerDto = {
  answer: string;
  sources: RagSource[];
};
