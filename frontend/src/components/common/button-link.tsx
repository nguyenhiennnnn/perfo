import type { ReactNode } from "react";
import type { VariantProps } from "class-variance-authority";
import { Link, type LinkProps } from "react-router";
import { Button, buttonVariants } from "@/components/ui/button";

type ButtonVariantProps = VariantProps<typeof buttonVariants>;

interface ButtonLinkProps extends Omit<LinkProps, "className" | "style" | "children"> {
  variant?: ButtonVariantProps["variant"];
  size?: ButtonVariantProps["size"];
  className?: string;
  children: ReactNode;
}

export const ButtonLink = ({ to, variant, size, className, children, ...linkProps }: ButtonLinkProps) => (
  <Button
    variant={variant}
    size={size}
    className={className}
    nativeButton={false}
    render={<Link to={to} {...linkProps} />}
  >
    {children}
  </Button>
);
