import React, { useState, useRef, useEffect, SelectHTMLAttributes } from "react";
import { cn } from "@shared/lib/cn";
import { ChevronDown, Check } from "lucide-react";

type SelectProps = SelectHTMLAttributes<HTMLSelectElement>;

export function Select({ className = "", children, value, onChange, title, ...props }: SelectProps) {
  const [isOpen, setIsOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);
  const [dropdownStyle, setDropdownStyle] = useState<React.CSSProperties>({});

  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    }
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  const handleToggle = () => {
    if (!isOpen && containerRef.current) {
      const rect = containerRef.current.getBoundingClientRect();
      const windowWidth = window.innerWidth;
      if (rect.left > windowWidth / 2) {
        setDropdownStyle({ right: 0, minWidth: "100%", width: "max-content", maxWidth: "350px" });
      } else {
        setDropdownStyle({ left: 0, minWidth: "100%", width: "max-content", maxWidth: "350px" });
      }
    }
    setIsOpen(!isOpen);
  };

  const options = React.Children.toArray(children)
    .filter(React.isValidElement)
    .map((child: any) => ({
      value: child.props.value,
      label: child.props.children,
      title: child.props.title,
    }));

  const selectedOption = options.find((opt) => opt.value === value) || options[0];

  const handleSelect = (newValue: string) => {
    setIsOpen(false);
    if (onChange) {
      onChange({ target: { value: newValue } } as any);
    }
  };

  return (
    <div ref={containerRef} className={cn("relative", className)}>
      <button
        type="button"
        title={title || selectedOption?.title}
        onClick={handleToggle}
        className="flex h-11 w-full items-center justify-between rounded-xl border border-border bg-card px-4 text-sm leading-5 text-foreground shadow-sm outline-none transition-[border-color,box-shadow,background-color] focus:border-primary focus:ring-2 focus:ring-primary/20 disabled:cursor-not-allowed disabled:bg-muted disabled:opacity-60"
        {...(props as any)}
      >
        <span className="truncate">{selectedOption?.label}</span>
        <ChevronDown size={16} className="ml-2 shrink-0 opacity-50" />
      </button>

      {isOpen && (
        <div 
          className="absolute z-50 mt-1 max-h-[400px] overflow-y-auto overflow-x-hidden rounded-xl border border-border bg-card p-1 text-sm shadow-xl animate-in fade-in slide-in-from-top-1"
          style={dropdownStyle}
        >
          {options.map((opt, i) => (
            <div
              key={i}
              onClick={() => handleSelect(opt.value)}
              className={cn(
                "relative flex w-full cursor-pointer select-none items-start justify-between gap-3 rounded-lg py-2 pl-3 pr-3 outline-none hover:bg-muted hover:text-foreground",
                opt.value === value ? "bg-muted font-medium" : "text-muted-foreground"
              )}
            >
              <span className="flex-1 break-words">{opt.label}</span>
              {opt.value === value && <Check size={14} className="mt-0.5 shrink-0" />}
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
