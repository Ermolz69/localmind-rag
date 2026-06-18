import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";
import rehypeHighlight from "rehype-highlight";
import "highlight.js/styles/github-dark.css";

type MarkdownPreviewProps = {
  markdown: string;
};

export function MarkdownPreview({ markdown }: MarkdownPreviewProps) {
  return (
    <div className="h-full overflow-y-auto bg-card px-6 py-5 pb-20">
      <div className="prose prose-sm max-w-3xl dark:prose-invert prose-headings:text-foreground prose-p:text-foreground prose-a:text-primary prose-blockquote:border-l-primary prose-blockquote:text-muted-foreground prose-strong:text-foreground prose-code:text-foreground prose-pre:border prose-pre:border-border prose-pre:bg-muted prose-th:border prose-th:border-border prose-td:border prose-td:border-border">
        <ReactMarkdown
          remarkPlugins={[remarkGfm]}
          rehypePlugins={[rehypeHighlight]}
        >
          {markdown}
        </ReactMarkdown>
      </div>
    </div>
  );
}
