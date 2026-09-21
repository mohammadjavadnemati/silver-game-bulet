"use client";

import Image from "next/image";
import { CARD_BACK_IMAGE, CARD_IMAGES, type CardType } from "@/lib/types";

export function DrawPileStack({
  count,
  cards,
  onClick,
  disabled,
}: {
  count: number;
  cards?: { cardId: string; type: string | null; value: number | null; isPubliclyRevealed: boolean }[];
  onClick: () => void;
  disabled?: boolean;
}) {
  const topCard = cards && cards.length > 0 ? cards[0] : null;
  const isTopRevealed = !!topCard?.isPubliclyRevealed && !!topCard.type;
  const topImageSrc = isTopRevealed ? CARD_IMAGES[topCard!.type as CardType] : CARD_BACK_IMAGE;

  return (
    <div className="flex flex-col items-center gap-1">
      <div
        onClick={disabled ? undefined : onClick}
        className={`relative w-20 h-28 sm:w-24 sm:h-32 ${disabled ? "opacity-50 cursor-not-allowed" : "cursor-pointer hover:brightness-110"} transition`}
      >
        <div className="absolute inset-0 translate-x-1.5 translate-y-1.5 rounded-md overflow-hidden -z-10 opacity-60">
          <Image src={CARD_BACK_IMAGE} alt="" fill className="object-cover" draggable={false} />
        </div>
        <div className="absolute inset-0 translate-x-3 translate-y-3 rounded-md overflow-hidden -z-20 opacity-30">
          <Image src={CARD_BACK_IMAGE} alt="" fill className="object-cover" draggable={false} />
        </div>
        <div className={`relative w-full h-full rounded-md overflow-hidden border ${isTopRevealed ? "border-ember" : "border-silver/30"}`}>
          <Image src={topImageSrc} alt="دسته اصلی" fill className="object-cover" draggable={false} />
          <div
            className="absolute inset-x-0 bottom-0 flex items-center justify-center py-1"
            style={{ background: "linear-gradient(to top, rgba(0,0,0,0.85), transparent)" }}
          >
            <span className="font-mono text-white text-sm">{count}</span>
          </div>
        </div>
      </div>
      <span className="text-[10px] text-silver/40">
        {isTopRevealed ? "دسته اصلی (برگشته)" : "دسته اصلی"}
      </span>
    </div>
  );
}