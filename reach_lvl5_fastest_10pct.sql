-- ClickHouse SQL
-- Задача:
-- 1) Для каждого игрока найти время от регистрации до первого достижения 5 уровня Штаба
--    (событие building_upgrade c level = 5).
-- 2) Вернуть user_id, platform, minutes_to_reach_lvl5.
-- 3) Оставить только самых быстрых игроков: тех, кто попал в топ-10% по скорости
--    достижения 5 уровня (то есть быстрее, чем примерно 90% остальных).

WITH lvl5_events AS (
    SELECT
        user_id,
        platform,
        acc_reg_ts,
        event_ts AS lvl5_ts,

        -- Если по игроку есть несколько событий с level = 5,
        -- берём самое раннее.
        row_number() OVER (
            PARTITION BY user_id
            ORDER BY event_ts ASC
        ) AS rn
    FROM user_events
    WHERE event_name = 'building_upgrade'
      AND level = 5
),

first_lvl5 AS (
    SELECT
        user_id,
        platform,

        -- Разница между регистрацией и первым достижением 5 уровня в минутах.
        dateDiff('minute', acc_reg_ts, lvl5_ts) AS minutes_to_reach_lvl5
    FROM lvl5_events
    WHERE rn = 1
),

ranked_users AS (
    SELECT
        user_id,
        platform,
        minutes_to_reach_lvl5,

        -- Кумулятивная доля игроков при сортировке по времени ASC:
        -- чем меньше значение, тем быстрее игрок достиг 5 уровня.
        -- Фильтр <= 0.10 оставит самых быстрых ~10% игроков.
        cume_dist() OVER (
            ORDER BY minutes_to_reach_lvl5 ASC
        ) AS speed_top_share
    FROM first_lvl5
)

SELECT
    user_id,
    platform,
    minutes_to_reach_lvl5
FROM ranked_users
WHERE speed_top_share <= 0.10
ORDER BY
    minutes_to_reach_lvl5 ASC,
    user_id ASC;
