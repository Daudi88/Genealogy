SELECT f.first_name as 'Namn', c.[name] as 'Stad', co.[name] as 'Land' 
FROM Family f
INNER JOIN Cities c ON f.birth_place_id = c.id
INNER JOIN Countries co ON c.country_id = co.id
WHERE f.id = 1