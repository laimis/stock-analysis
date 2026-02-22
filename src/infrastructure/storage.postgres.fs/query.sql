-- file for random queries
-- not intended to be used in production, just for testing and development purposes

select * from sec_filings 
order by created_at desc
limit 10;


-- top sec filings by form type
select form_type, count(*) as count
from sec_filings
group by form_type
order by count desc
limit 10;

