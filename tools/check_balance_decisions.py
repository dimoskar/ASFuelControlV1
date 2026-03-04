#!/usr/bin/env python3
from dataclasses import dataclass

LOCK_MINUTES_BEFORE_MIDNIGHT = 5

@dataclass
class DecisionInput:
    month_balance_enabled: bool
    month_balance_ok: bool
    has_current_balance: bool
    minutes_to_midnight: int
    has_gap: bool
    has_any_balances: bool


def current_balance_ok_after_time_window(has_current_balance: bool, minutes_to_midnight: int) -> bool:
    if has_current_balance:
        return True
    return minutes_to_midnight > LOCK_MINUTES_BEFORE_MIDNIGHT


def decide(i: DecisionInput):
    current_balance_ok = current_balance_ok_after_time_window(i.has_current_balance, i.minutes_to_midnight)
    if i.month_balance_enabled and not i.month_balance_ok:
        return "month", "create"
    if not current_balance_ok:
        return "current-day", "create"
    if not i.has_any_balances:
        return "none", "return-null"
    if i.has_gap:
        return "missing-day", "create"
    return "none", "return-null"


rows = [
    ("R1", DecisionInput(True, False, True, 90, False, True), ("month", "create")),
    ("R2", DecisionInput(False, True, False, 4, False, True), ("current-day", "create")),
    ("R3", DecisionInput(False, True, False, 6, True, True), ("missing-day", "create")),
    ("R4", DecisionInput(False, True, True, 30, False, True), ("none", "return-null")),
    ("R5", DecisionInput(False, True, True, 30, False, False), ("none", "return-null")),
]

for row_id, data, expected in rows:
    actual = decide(data)
    assert actual == expected, f"{row_id}: expected {expected}, got {actual}"
    print(f"{row_id}: PASS -> {actual}")

print("All decision-table rows validated.")
